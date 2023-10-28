using Unity.Entities;

namespace component.strategy.buildings
{
    public struct BuildingIdGenerator : IComponentData
    {
        public long nextIdToBeUsed;
        public long nextCompanyIdToBeUsed;
    }
}