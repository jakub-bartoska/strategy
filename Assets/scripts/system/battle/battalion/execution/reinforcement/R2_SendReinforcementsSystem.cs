using component;
using component._common.system_switchers;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
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
            var blockers = MovementDataHolder.blockers;
            var needReinforcements = DataHolder.needReinforcements;
            var reinforcements = DataHolder.reinforcements;
            var movingBattalions = MovementDataHolder.movingBattalions;
            var plannedMovementDirections = MovementDataHolder.plannedMovementDirections;
            var battalionsPerformingAction = DataHolder.battalionsPerformingAction;

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
            public NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers;
            public NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements;
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

                //follower format: blockerId, shadow/battalion ,direction
                foreach (var blocker in blockers.GetValuesForKey(battalionMarker.id))
                {
                    //can reinforce only same team
                    if (team.value != blocker.Item4)
                    {
                        continue;
                    }

                    //cannot reinforce shadow
                    if (blocker.Item2 == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    //can reinforce only in my default direction of move
                    if (blocker.Item3 != plannedMovementDirections[battalionMarker.id])
                    {
                        continue;
                    }

                    if (!needReinforcements.ContainsKey(blocker.Item1))
                    {
                        continue;
                    }

                    //send reinforcement                    
                    prepareReinforcements(blocker.Item1, ref soldiers);
                }
            }

            private void prepareReinforcements(long needHelpBattalionId, ref DynamicBuffer<BattalionSoldiers> soldiers)
            {
                var indexesToSend = new NativeList<int>(Allocator.Temp);
                foreach (var index in needReinforcements.GetValuesForKey(needHelpBattalionId))
                {
                    indexesToSend.Add(index);
                }

                indexesToSend.Sort();

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
                        if (reinforcementsUpdated(soldiersMap, index + i, ref soldiers, needHelpBattalionId)) break;
                        if (reinforcementsUpdated(soldiersMap, index - i, ref soldiers, needHelpBattalionId)) break;
                    }
                }
            }

            private bool reinforcementsUpdated(NativeHashMap<int, (BattalionSoldiers, int)> soldiersMap, int index, ref DynamicBuffer<BattalionSoldiers> soldiers, long needHelpBattalionId)
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
                    reinforcements.Add(needHelpBattalionId, newSoldier);
                    return true;
                }

                return false;
            }
        }
    }
}