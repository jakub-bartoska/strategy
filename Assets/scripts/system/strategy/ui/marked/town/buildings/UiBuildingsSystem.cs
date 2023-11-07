using _Monobehaviors.town_buildings_ui;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.buildings;
using component.strategy.buildings.building_costs;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked.town.buildings
{
    public partial struct UiBuildingsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<InterfaceState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var interfaceState = SystemAPI.GetSingletonRW<InterfaceState>();
            if (interfaceState.ValueRO.state != UIState.TOWN_BUILDINGS_UI) return;

            var buildingCosts = new NativeParallelMultiHashMap<BuildingCostTag, ResourceHolder>(30, Allocator.TempJob);
            new CollectBuildingCosts
                {
                    buildingCosts = buildingCosts
                }.Schedule(state.Dependency)
                .Complete();

            var existingBuildings = new NativeList<BuildingType>(Allocator.TempJob);
            new CollectExistingBuildings
                {
                    result = existingBuildings
                }.Schedule(state.Dependency)
                .Complete();

            var keys = buildingCosts.GetKeyArray(Allocator.TempJob);
            var uniqueCount = keys.Unique();
            var uniqueKeys = keys.GetSubArray(0, uniqueCount);

            foreach (var buildingCostTag in uniqueKeys)
            {
                var res = buildingCosts.GetValuesForKey(buildingCostTag);
                var resList = new NativeList<ResourceHolder>(Allocator.TempJob);
                foreach (var resourceHolder in res)
                {
                    resList.Add(resourceHolder);
                }

                var hasTownBuilding = false;
                foreach (var existingBuilding in existingBuildings)
                {
                    if (existingBuilding == buildingCostTag.buildingType) hasTownBuilding = true;
                }

                BuildingDisplayer.instance.displayBuilding(buildingCostTag.buildingType, resList, hasTownBuilding);
            }
        }

        [BurstCompile]
        public partial struct CollectBuildingCosts : IJobEntity
        {
            public NativeParallelMultiHashMap<BuildingCostTag, ResourceHolder> buildingCosts;

            private void Execute(BuildingCostTag tag, DynamicBuffer<ResourceHolder> resourceCosts)
            {
                foreach (var resourceCost in resourceCosts)
                {
                    buildingCosts.Add(tag, resourceCost);
                }
            }
        }

        [BurstCompile]
        public partial struct CollectExistingBuildings : IJobEntity
        {
            public NativeList<BuildingType> result;

            private void Execute(TownTag tag, Marked marked, DynamicBuffer<ExistingBuildingBuffer> existingBuildings)
            {
                foreach (var building in existingBuildings)
                {
                    result.Add(building.type);
                }
            }
        }
    }
}