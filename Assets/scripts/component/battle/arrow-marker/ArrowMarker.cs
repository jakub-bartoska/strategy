using Unity.Entities;
using Unity.Mathematics;

namespace component.authoring_pairs
{
    public struct ArrowMarker : IComponentData
    {
        public Team team;
        public float3 direction;
        public float lifeRemaining;
    }
}