using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
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
            var blockers = DataHolder.blockers;
            var needReinforcements = DataHolder.needReinforcements;
            var reinforcements = DataHolder.reinforcements;

            new UpdateReinforcementsJob
                {
                    needReinforcements = needReinforcements,
                    blockers = blockers,
                    reinforcements = reinforcements
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct UpdateReinforcementsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, int> needReinforcements;
            public NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers;
            public NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements;

            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection, BattalionTeam team, ref DynamicBuffer<BattalionSoldiers> soldiers,
                ref BattalionHealth health)
            {
                //only not moving battalions can send reinforcements
                if (movementDirection.currentDirection != Direction.NONE)
                {
                    return;
                }

                //fighting battalions cannot send reinforcements
                if (DataHolder.fightingBattalions.Contains(battalionMarker.id))
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
                    if (blocker.Item3 != movementDirection.plannedDirection)
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