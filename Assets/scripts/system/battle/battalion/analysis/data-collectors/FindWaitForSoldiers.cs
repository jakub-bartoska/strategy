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
    public partial struct FindWaitForSoldiers : ISystem
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
            var waitingForSoldiersBattalions = movementDataHolder.ValueRW.waitingForSoldiersBattalions;
            new CollectBattleUnitPositionsJob
                {
                    waitingForSoldiersBattalions = waitingForSoldiersBattalions
                }.Schedule(state.Dependency)
                .Complete();

            foreach (var waitingForSoldiersBattalion in waitingForSoldiersBattalions)
            {
                dataHolder.ValueRW.battalionsPerformingAction.Add(waitingForSoldiersBattalion);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(WaitForSoldiersTag))]
    public partial struct CollectBattleUnitPositionsJob : IJobEntity
    {
        public NativeHashSet<long> waitingForSoldiersBattalions;

        private void Execute(BattalionMarker battalionMarker)
        {
            waitingForSoldiersBattalions.Add(battalionMarker.id);
        }
    }
}