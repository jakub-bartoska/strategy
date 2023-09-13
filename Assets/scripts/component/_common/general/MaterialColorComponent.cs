using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace component.general
{
    [MaterialProperty("_BaseColor")]
    public struct MaterialColorComponent : IComponentData
    {
        public float4 Value;
    }
}