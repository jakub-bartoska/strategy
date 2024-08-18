using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution.reinforcement
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(R1_RemoveMovingBattalionsSystem))]
    public partial struct R2_SendReinforcementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();

            var blockers = movementDataHolder.ValueRO.blockers;
            var needReinforcements = dataHolder.ValueRO.needReinforcements;
            var reinforcements = dataHolder.ValueRO.reinforcements;
            var movingBattalions = movementDataHolder.ValueRO.movingBattalions;
            var plannedMovementDirections = movementDataHolder.ValueRO.plannedMovementDirections;
            var battalionsPerformingAction = dataHolder.ValueRO.battalionsPerformingAction;

            new UpdateReinforcementsJob
                {
                    needReinforcements = needReinforcements,
                    blockers = blockers,
                    reinforcements = reinforcements,
                    movingBattalions = movingBattalions,
                    plannedMovementDirections = plannedMovementDirections,
                    battalionsPerformingAction = battalionsPerformingAction
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct UpdateReinforcementsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, int> needReinforcements;
            public NativeParallelMultiHashMap<long, BattalionBlocker> blockers;
            public NativeParallelMultiHashMap<long, Reinforcements> reinforcements;
            public NativeHashMap<long, Direction> movingBattalions;
            public NativeHashMap<long, Direction> plannedMovementDirections;
            public NativeHashSet<long> battalionsPerformingAction;

            private void Execute(BattalionMarker battalionMarker, BattalionTeam team, ref DynamicBuffer<BattalionSoldiers> soldiers,
                ref BattalionHealth health)
            {
                //only not moving battalions can send reinforcements
                if (movingBattalions.ContainsKey(battalionMarker.id))
                {
                    return;
                }

                //battalions alrerady performing any action should not send reinforcements
                if (battalionsPerformingAction.Contains(battalionMarker.id))
                {
                    return;
                }

                //blocker format: blockerId, shadow/battalion ,direction
                foreach (var blocker in blockers.GetValuesForKey(battalionMarker.id))
                {
                    //can reinforce only same team
                    if (team.value != blocker.team)
                    {
                        continue;
                    }

                    //cannot reinforce shadow
                    if (blocker.blockerType == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    //can reinforce only in my current direction of move
                    if (plannedMovementDirections.TryGetValue(battalionMarker.id, out var direction))
                    {
                        if (blocker.blockingDirection != direction)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (!needReinforcements.ContainsKey(blocker.blockerId))
                    {
                        continue;
                    }

                    //send reinforcement                    
                    prepareReinforcements(blocker.blockerId, ref soldiers, battalionMarker.id);
                }
            }

            private void prepareReinforcements(long needHelpBattalionId, ref DynamicBuffer<BattalionSoldiers> soldiers, long myBattalionId)
            {
                //all indexes missing in target battalion
                var indexesToSend = new NativeList<int>(Allocator.Temp);
                foreach (var index in needReinforcements.GetValuesForKey(needHelpBattalionId))
                {
                    indexesToSend.Add(index);
                }

                indexesToSend.Sort();

                //soldier position - (soldier, index in soldiers array)
                var soldiersMap = new NativeHashMap<int, (BattalionSoldiers, int)>(soldiers.Length, Allocator.Temp);

                foreach (var index in indexesToSend)
                {
                    soldiersMap.Clear();
                    for (var i = 0; i < soldiers.Length; i++)
                    {
                        soldiersMap.Add(soldiers[i].positionWithinBattalion, (soldiers[i], i));
                    }

                    for (var i = 0; i < 10; i++)
                    {
                        if (reinforcementsUpdated(soldiersMap, index + i, ref soldiers, needHelpBattalionId, myBattalionId)) break;
                        if (reinforcementsUpdated(soldiersMap, index - i, ref soldiers, needHelpBattalionId, myBattalionId)) break;
                    }
                }
            }

            private bool reinforcementsUpdated(NativeHashMap<int, (BattalionSoldiers, int)> soldiersMap,
                int index,
                ref DynamicBuffer<BattalionSoldiers> soldiers,
                long needHelpBattalionId,
                long myBattalionId)
            {
                if (soldiersMap.ContainsKey(index))
                {
                    var soldier = soldiersMap[index];
                    var newSoldier = new BattalionSoldiers
                    {
                        soldierId = soldier.Item1.soldierId,
                        positionWithinBattalion = index,
                        entity = soldier.Item1.entity
                    };
                    soldiers.RemoveAt(soldier.Item2);
                    reinforcements.Add(needHelpBattalionId, new Reinforcements
                    {
                        reinforcement = newSoldier,
                        originalBattalionId = myBattalionId,
                        originalPosition = soldier.Item1.positionWithinBattalion
                    });
                    return true;
                }

                return false;
            }
        }
    }
}