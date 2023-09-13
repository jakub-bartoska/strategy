using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.authoring_pairs;
using component.helpers.positioning;
using system_groups;
using system.behaviors.behavior_systems.shoot_arrow.aspect;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.behaviors.behavior_systems.shoot_arrow
{
    [BurstCompile]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct ShootArrowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PositionHolder>();
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var arrowConfig = SystemAPI.GetSingleton<ArrowConfig>();
            var arrowPrefab = SystemAPI.GetSingleton<PrefabHolder>().arrowPrefab;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var positionHolder = SystemAPI.GetSingleton<PositionHolder>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            state.Dependency = new ShootArrowJob
            {
                arrowPrefab = arrowPrefab,
                ecb = ecb.AsParallelWriter(),
                deltaTime = deltaTime,
                arrowConfig = arrowConfig,
                positionHolder = positionHolder,
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct ShootArrowJob : IJobEntity
    {
        [ReadOnly] public Entity arrowPrefab;
        public EntityCommandBuffer.ParallelWriter ecb;
        public float deltaTime;
        [ReadOnly] public ArrowConfig arrowConfig;
        [ReadOnly] public PositionHolder positionHolder;

        private void Execute(ShootArrowAspect aspect)
        {
            aspect.execute(ecb, arrowPrefab, deltaTime, arrowConfig, positionHolder);
        }
    }
}