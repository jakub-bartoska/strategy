using Unity.Entities;

namespace component.strategy.buildings.building_costs
{
    public struct BuildingCostTag : IComponentData
    {
        public BuildingType BuildingType;
    }
}