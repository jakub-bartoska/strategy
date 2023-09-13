using Unity.Entities;
using Unity.Mathematics;

namespace component.strategy.general
{
    public struct SelectionMarkerState : IComponentData
    {
        public MarkerState state;
        public float3 min;
        public float3 max;
        public float2 min2D;
        public float2 max2D;
    }

    public enum MarkerState
    {
        RUNNING,
        FINISHED,
        IDLE,
        DISABLED
    }
}