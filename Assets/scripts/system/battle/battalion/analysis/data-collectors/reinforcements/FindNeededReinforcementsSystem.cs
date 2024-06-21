using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.reinforcements
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    public partial struct FindNeededReinforcementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var needReinforcements = dataHolder.ValueRW.needReinforcements;

            new CollectBattalionsNeedingReinforcementsJob
                {
                    battalionIdsToMissingIndexes = needReinforcements
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectBattalionsNeedingReinforcementsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, int> battalionIdsToMissingIndexes;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers)
            {
                if (soldiers.Length == 10) return;

                for (var i = 0; i < 10; i++)
                {
                    var exists = false;
                    foreach (var soldier in soldiers)
                    {
                        if (soldier.positionWithinBattalion == i)
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
    }
}