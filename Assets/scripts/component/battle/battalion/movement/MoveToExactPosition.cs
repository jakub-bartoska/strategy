using Unity.Entities;
using Unity.Mathematics;

namespace component.battle.battalion.markers
{
    public struct MoveToExactPosition : IComponentData
    {
        public float3 targetPosition;
    }
}