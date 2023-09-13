using component.config.game_settings;
using Unity.Entities;

namespace component.strategy.army_components
{
    public struct ArmyCompany : IBufferElementData
    {
        public long id;
        public int soldierCount;
        public SoldierType type;
    }
    
}