using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion
{
    public partial struct RemoveEmptyBattalionsSystem : ISystem
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
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new DestroyEmptyBattalionsJob
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct DestroyEmptyBattalionsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers, BattalionHealth health, Entity entity)
            {
                if (soldiers.Length == 0 || health.value <= 0)
                    ecb.DestroyEntity(0, entity);
            }
        }
    }
}