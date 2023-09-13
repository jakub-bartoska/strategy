using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.strategy.ui
{
    public struct StrategyUiLabel : IComponentData
    {
        public long id;
        public float3 position;
        public FixedString64Bytes text;
    }
}