using component._common.system_switchers;
using system.positions.path_tracker.aspect;
using Unity.Burst;
using Unity.Entities;

namespace system.positions.path_tracker
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PathTrackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            new PositionTrackerJob
            {
                deltaTime = deltaTime
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct PositionTrackerJob : IJobEntity
    {
        public float deltaTime;

        [BurstCompile]
        private void Execute(PathtrackingAspect aspect)
        {
            aspect.execute(deltaTime);
        }
    }
}