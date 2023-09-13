using Unity.Entities;
using Unity.Mathematics;

namespace component.pathfinding
{
    public struct PathTracker : IComponentData
    {
        public bool isMoving;
        public float3 oldPosition;
        public float timerRemaining;
        public float defaultTimer;
    }
}