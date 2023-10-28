using Unity.Entities;

namespace component.strategy.buildings
{
    public struct Building : IBufferElementData
    {
        public long id;
        public BuildingType type;
        public int level;
    }

    public enum BuildingType
    {
        ARCHERY,
        STABLES,
        BARRACKS,
        TOWN_HALL
    }
}