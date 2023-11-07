using component.strategy.buildings;
using Unity.Entities;

namespace component.strategy.buy_army
{
    public struct BuildingPurchase : IBufferElementData
    {
        public BuildingType type;
    }
}