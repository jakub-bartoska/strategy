using component._common.config.camera;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.authoring_pairs;
using component.config.game_settings;
using Unity.Burst;
using Unity.Entities;

namespace system._common
{
    public partial struct InitSystemSwitcherSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<InitState>();
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<TeamColor>();
            state.RequireForUpdate<CameraConfigComponentData>();
            state.RequireForUpdate<SystemStatusHolder>();
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<TeamPositions>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var systemStatusHolder = SystemAPI.GetSingletonRW<SystemStatusHolder>();
            var initState = SystemAPI.GetSingleton<InitState>();

            systemStatusHolder.ValueRW.desiredStatus = initState.desiredStatus;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var initStateEntity = SystemAPI.GetSingletonEntity<InitState>();
            ecb.DestroyEntity(initStateEntity);
        }
    }
}