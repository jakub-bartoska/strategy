using component._common.system_switchers;
using component.config.authoring_pairs;
using system.projectiles.arrows.aspect;
using Unity.Burst;
using Unity.Entities;

namespace system.projectiles.arrows
{
    [BurstCompile]
    public partial struct ArrowFlightSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var arrowConfig = SystemAPI.GetSingleton<ArrowConfig>();

            new ArrowFlightJob
                {
                    deltaTime = deltaTime,
                    ecb = ecb,
                    arrowConfig = arrowConfig
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct ArrowFlightJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer ecb;
        public ArrowConfig arrowConfig;

        [BurstCompile]
        private void Execute(ArrowFlightAspect aspect)
        {
            aspect.execute(deltaTime, ecb, arrowConfig);
        }
    }
}