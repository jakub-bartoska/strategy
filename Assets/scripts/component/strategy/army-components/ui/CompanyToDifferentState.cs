using Unity.Entities;

namespace component.strategy.army_components.ui
{
    public struct CompanyToDifferentState : IBufferElementData
    {
        public long companyId;
        public CompanyState targetState;
    }

    public enum CompanyState
    {
        TOWN_TO_DEPLOY,
        TOWN
    }
}