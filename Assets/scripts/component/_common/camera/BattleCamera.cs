using Unity.Entities;
using Unity.Mathematics;

namespace component._common.camera
{
    public struct BattleCamera : IComponentData
    {
        public float3 desiredPosition;
    }
}