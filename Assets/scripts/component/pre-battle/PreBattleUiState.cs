using component.config.game_settings;
using Unity.Entities;

namespace component.pre_battle
{
    public struct PreBattleUiState : IComponentData
    {
        public Team selectedTeam;
        public SoldierType? selectedCard;

        public PreBattleEvent preBattleEvent;
    }

    public enum PreBattleEvent
    {
        NONE,
        INIT,
        TEAM_CHANGED,
        CARD_CHANGED
    }
}