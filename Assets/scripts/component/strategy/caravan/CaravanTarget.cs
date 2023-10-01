using Unity.Entities;

namespace component.strategy.caravan
{
    public struct CaravanTarget : IComponentData
    {
        public long targetId;
    }
}