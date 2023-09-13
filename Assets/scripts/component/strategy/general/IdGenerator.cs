using Unity.Entities;

namespace component.strategy.general
{
    public struct IdGenerator : IComponentData
    {
        public long nextIdToBeUsed;
        public long nextCompanyIdToBeUsed;
    }
}