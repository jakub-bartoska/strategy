using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    public partial struct AllBattalionIdsCollector : ISystem
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
            var allBattalionIds = dataHolder.ValueRW.allBattalionIds;

            new CollectBattleUnitPositionsJob
                {
                    allBattalionIds = allBattalionIds
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectBattleUnitPositionsJob : IJobEntity
        {
            public NativeHashSet<long> allBattalionIds;

            private void Execute(BattalionMarker battalionMarker)
            {
                allBattalionIds.Add(battalionMarker.id);
            }
        }
    }
}