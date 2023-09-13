using component.pathfinding;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.positions.path_tracker.aspect
{
    public readonly partial struct PathtrackingAspect : IAspect
    {
        private readonly RefRO<LocalTransform> transform;
        private readonly RefRW<PathTracker> pathTracker;

        public void execute(float deltaTime)
        {
            pathTracker.ValueRW.timerRemaining -= deltaTime;
            if (pathTracker.ValueRO.timerRemaining > 0)
            {
                return;
            }

            var currentPosition = transform.ValueRO.Position;
            var time = math.abs(pathTracker.ValueRO.timerRemaining - pathTracker.ValueRO.defaultTimer);
            var speed =
                math.length(pathTracker.ValueRO.oldPosition - currentPosition) / time;

            pathTracker.ValueRW.isMoving = speed > 5;

            pathTracker.ValueRW.oldPosition = currentPosition;
            pathTracker.ValueRW.timerRemaining = pathTracker.ValueRO.defaultTimer;
        }
    }
}