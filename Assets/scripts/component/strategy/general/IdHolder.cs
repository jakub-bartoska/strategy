using Unity.Entities;

namespace component.strategy.general
{
    public struct IdHolder : IComponentData
    {
        public long id;
        public HolderType type;
    }

    public enum HolderType
    {
        ARMY,
        TOWN,
        TOWN_DEPLOYER
    }
}