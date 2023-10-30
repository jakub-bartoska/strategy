using _Monobehaviors.town_buildings_ui;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.buildings.building_costs;
using component.strategy.player_resources;
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

                BuildingDisplayer.instance.displayBuilding(buildingCostTag.buildingType, resList);
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
    }
}