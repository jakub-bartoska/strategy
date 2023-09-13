using component._common.system_switchers;
using component.config.authoring_pairs;
using Unity.Burst;
using Unity.Entities;

namespace system
{
    [BurstCompile]
    public partial struct BehaviorPickerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var arrowConfig = SystemAPI.GetSingleton<ArrowConfig>();
            new PickBehaviorJob
                {
                    arrowConfig = arrowConfig,
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    [BurstCompile]
    public partial struct PickBehaviorJob : IJobEntity
    {
        public ArrowConfig arrowConfig;

        private void Execute(BehaviorPickerAspect behaviorPickerAspect)
        {
            behaviorPickerAspect.pickBestBehavior(
                arrowConfig
            );
        }
    }
}