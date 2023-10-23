using component.config.game_settings;
using Unity.Entities;

namespace component.strategy.buy_army
{
    public struct ArmyPurchase : IBufferElementData
    {
        public SoldierType type;
        public int count;
    }
}