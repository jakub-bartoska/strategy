using component.config.game_settings;
using Unity.Entities;

namespace component.pre_battle.cards
{
    public struct CardInfo : IBufferElementData
    {
        public Team team;
        public SoldierType soldierType;
        public int maxBattalionCount;
        public int currentBattalionCount;
    }
}