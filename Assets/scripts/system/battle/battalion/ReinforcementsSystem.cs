using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(BattalionMovementSystem))]
    [UpdateAfter(typeof(BattalionFightSystem))]
    public partial struct ReinforcementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var battalionIdsToMissingIndexes = new NativeParallelMultiHashMap<long, int>(3000, Allocator.TempJob);
            new CollectBattalionsNeedingReinforcementsJob
                {
                    battalionIdsToMissingIndexes = battalionIdsToMissingIndexes.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            if (battalionIdsToMissingIndexes.Count() == 0) return;

            var possibleReinforcements = SystemAPI.GetSingletonBuffer<PossibleReinforcements>();
            var reinforcements = new NativeParallelMultiHashMap<long, BattalionSoldiers>(3000, Allocator.TempJob);

            new SendReinforcementsJob
                {
                    reinforcements = reinforcements.AsParallelWriter(),
                    battalionIdsToMissingSoldiersCount = battalionIdsToMissingIndexes,
                    possibleReinforcements = possibleReinforcements
                }.ScheduleParallel(state.Dependency)
                .Complete();

            new ReceiveReinforcementsJob
                {
                    reinforcements = reinforcements
                }.ScheduleParallel(state.Dependency)
                .Complete();
            possibleReinforcements.Clear();
        }
    }

    [BurstCompile]
    public partial struct CollectBattalionsNeedingReinforcementsJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, int>.ParallelWriter battalionIdsToMissingIndexes;

        private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers)
        {
            if (soldiers.Length == 10) return;

            for (var i = 0; i < 10; i++)
            {
                var exists = false;
                foreach (var soldier in soldiers)
                {
                    if (soldier.position == i)
                    {
                        exists = true;
                    }
                }

                if (!exists)
                {
                    battalionIdsToMissingIndexes.Add(battalionMarker.id, i);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct SendReinforcementsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<long, int> battalionIdsToMissingSoldiersCount;
        public NativeParallelMultiHashMap<long, BattalionSoldiers>.ParallelWriter reinforcements;
        [ReadOnly] public DynamicBuffer<PossibleReinforcements> possibleReinforcements;

        private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionSoldiers> soldiers)
        {
            foreach (var possibleReinforcement in possibleReinforcements)
            {
                if (possibleReinforcement.canHelpBattalionId != battalionMarker.id) continue;

                var indexesToSend = new NativeList<int>(Allocator.Temp);
                foreach (var index in battalionIdsToMissingSoldiersCount.GetValuesForKey(possibleReinforcement.needHelpBattalionId))
                {
                    indexesToSend.Add(index);
                }

                indexesToSend.Sort();

                // position -> (soldiers, index)
                var soldiersMap = new NativeHashMap<int, (BattalionSoldiers, int)>(soldiers.Length, Allocator.Temp);

                foreach (var index in indexesToSend)
                {
                    soldiersMap.Clear();
                    for (var i = 0; i < soldiers.Length; i++)
                    {
                        if (soldiers[i].position == index)
                        {
                            var newSoldier = new BattalionSoldiers
                            {
                                soldierId = soldiers[i].soldierId,
                                position = index,
                                entity = soldiers[i].entity
                            };
                            soldiers.RemoveAt(i);
                            reinforcements.Add(possibleReinforcement.needHelpBattalionId, newSoldier);
                            goto outerLoop;
                        }

                        soldiersMap.Add(soldiers[i].position, (soldiers[i], i));
                    }

                    for (var i = 0; i < 10; i++)
                    {
                        if (soldiersMap.ContainsKey(index + i))
                        {
                            var hm = soldiersMap[index + i];
                            var newSoldier = new BattalionSoldiers
                            {
                                soldierId = hm.Item1.soldierId,
                                position = index,
                                entity = hm.Item1.entity
                            };
                            soldiers.RemoveAt(hm.Item2);
                            reinforcements.Add(possibleReinforcement.needHelpBattalionId, newSoldier);
                            break;
                        }

                        if (soldiersMap.ContainsKey(index - i))
                        {
                            var hm = soldiersMap[index - i];
                            var newSoldier = new BattalionSoldiers
                            {
                                soldierId = hm.Item1.soldierId,
                                position = index,
                                entity = hm.Item1.entity
                            };
                            soldiers.RemoveAt(hm.Item2);
                            reinforcements.Add(possibleReinforcement.needHelpBattalionId, newSoldier);
                            break;
                        }
                    }

                    outerLoop:
                    continue;
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ReceiveReinforcementsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements;

        private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionSoldiers> soldiers, ref BattalionHealth health)
        {
            if (!reinforcements.ContainsKey(battalionMarker.id)) return;

            var healthIncrease = 0;
            foreach (var reinforcement in reinforcements.GetValuesForKey(battalionMarker.id))
            {
                healthIncrease += 10;
                soldiers.Add(reinforcement);
            }

            health.value += healthIncrease;
        }
    }
}