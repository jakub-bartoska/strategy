using Unity.Entities;
using Unity.Mathematics;

namespace component.authoring_pairs
{
    public struct ArrowMarkerDebug : IComponentData
    {
        public float3 startingPosition;
        public float yForce;
    }
}