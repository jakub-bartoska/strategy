using Unity.Entities;

namespace component._common.system_switchers
{
    public struct SystemStatusHolder : IComponentData
    {
        public SystemStatus currentStatus;
        public SystemStatus desiredStatus;
        public SystemStatus previousStatus;
    }

    public enum SystemStatus
    {
        NO_STATUS,
        STRATEGY,
        BATTLE_PLAN,
        BATTLE,
        MENU,
        INGAME_MENU,
        RESTART
    }
}