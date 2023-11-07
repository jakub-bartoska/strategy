using component._common.system_switchers;
using component.strategy.buildings;
using component.strategy.buildings.building_costs;
using component.strategy.buy_army;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using system.strategy.ui.marked.town.buildings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.town
{
    public partial struct BuildingPurchaseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<BuildingPurchase>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buildingPurchases = SystemAPI.GetSingletonBuffer<BuildingPurchase>();
            if (buildingPurchases.IsEmpty) return;

            var buildingCosts = new NativeParallelMultiHashMap<BuildingCostTag, ResourceHolder>(30, Allocator.TempJob);
            new UiBuildingsSystem.CollectBuildingCosts
                {
                    buildingCosts = buildingCosts
                }.Schedule(state.Dependency)
                .Complete();

            //nacist cenu budov
            foreach (var buildingPurchase in buildingPurchases)
            {
                var costs = new NativeList<ResourceHolder>(Allocator.TempJob);
                foreach (var buildingCost in buildingCosts)
                {
                    if (buildingCost.Key.buildingType == buildingPurchase.type)
                    {
                        costs.Add(buildingCost.Value);
                    }
                }

                new PurchaseNewBuildingJob
                    {
                        buildingPurchase = buildingPurchase,
                        buildingCosts = costs
                    }.Schedule(state.Dependency)
                    .Complete();
            }

            buildingPurchases.Clear();
        }

        public partial struct PurchaseNewBuildingJob : IJobEntity
        {
            public BuildingPurchase buildingPurchase;
            public NativeList<ResourceHolder> buildingCosts;

            private void Execute(Marked marked, TownTag townTag, ref DynamicBuffer<ResourceHolder> resources, ref DynamicBuffer<ExistingBuildingBuffer> existingBuildings)
            {
                foreach (var existingBuilding in existingBuildings)
                {
                    if (existingBuilding.type == buildingPurchase.type) return;
                }

                if (!canPurchase(resources)) return;

                for (int i = 0; i < resources.Length; i++)
                {
                    foreach (var costResource in buildingCosts)
                    {
                        if (resources[i].type != costResource.type) continue;

                        var resource = resources[i];
                        resource.value -= costResource.value;
                        resources[i] = resource;
                    }
                }

                existingBuildings.Add(new ExistingBuildingBuffer
                {
                    type = buildingPurchase.type
                });
            }

            private bool canPurchase(DynamicBuffer<ResourceHolder> resources)
            {
                foreach (var costResource in buildingCosts)
                {
                    foreach (var resourceHolder in resources)
                    {
                        if (costResource.type != resourceHolder.type) continue;

                        if (costResource.value > resourceHolder.value) return false;
                    }
                }

                return true;
            }
        }
    }
}