using component._common.system_switchers;
using component.battle.battalion;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    [UpdateAfter(typeof(DestroyKilledSoldiersSystem))]
    public partial struct RemoveEmptyBattalionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new DestroyEmptyBattalionsJob
                {
                    ecb = ecb
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(BattalionMarker))]
        public partial struct DestroyEmptyBattalionsJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            private void Execute(DynamicBuffer<BattalionSoldiers> soldiers, Entity entity)
            {
                if (soldiers.Length == 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}