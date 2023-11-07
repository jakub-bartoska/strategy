using Unity.Entities;

namespace component.strategy.buildings
{
    public struct ExistingBuildingBuffer : IBufferElementData
    {
        public BuildingType type;
    }
}