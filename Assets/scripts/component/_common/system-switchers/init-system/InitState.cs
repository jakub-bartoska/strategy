using Unity.Entities;

namespace component._common.system_switchers
{
    public struct InitState : IComponentData
    {
        public SystemStatus desiredStatus;
    }
}