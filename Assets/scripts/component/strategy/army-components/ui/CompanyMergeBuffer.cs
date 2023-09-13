using Unity.Entities;

namespace component.strategy.army_components.ui
{
    public struct CompanyMergeBuffer : IBufferElementData
    {
        public long companyId1;
        public long companyId2;
    }
}