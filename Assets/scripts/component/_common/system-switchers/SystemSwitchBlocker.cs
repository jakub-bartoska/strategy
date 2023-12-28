using Unity.Entities;

namespace component._common.system_switchers
{
    public struct SystemSwitchBlocker : IBufferElementData
    {
        public Blocker blocker;
    }

    public enum Blocker
    {
        ARMIES_MONO_TO_ENTITY,
        SPAWN_ARMY,
        AUTO_ADD_BLOCKERS,
        STOP_STRATEGY_MOVEMENT,
        ACTIVATE_STRATEGY_MOVEMENT,
        STOP_BATTLE_MOVEMENT,
        ACTIVATE_BATTLE_MOVEMENT,
        CLEAN_STRATEGY,
        CLEAN_BATTLE,
        SPAWN_STRATEGY,
        CAMERA_SWITCH,
        COMPANY_TO_BATTALION,
    }
}