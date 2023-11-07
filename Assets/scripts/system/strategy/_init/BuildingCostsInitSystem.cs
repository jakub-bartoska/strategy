using _Monobehaviors.ui.player_resources;
using component._common.system_switchers;
using component.strategy.buildings;
using component.strategy.buildings.building_costs;
using component.strategy.player_resources;
using Unity.Burst;
using Unity.Entities;

namespace system.strategy._init
{
    public partial struct BuildingCostsInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var archeryEntity = ecb.CreateEntity();
            ecb.AddComponent(archeryEntity, new BuildingCostTag
            {
                buildingType = BuildingType.ARCHERY
            });
            var archeryCosts = ecb.AddBuffer<ResourceHolder>(archeryEntity);
            archeryCosts.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 300
            });
            archeryCosts.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 200
            });


            var stablesEntity = ecb.CreateEntity();
            ecb.AddComponent(stablesEntity, new BuildingCostTag
            {
                buildingType = BuildingType.STABLES
            });
            var stablesCosts = ecb.AddBuffer<ResourceHolder>(stablesEntity);
            stablesCosts.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 500
            });
            stablesCosts.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 300
            });

            var barracksEntity = ecb.CreateEntity();
            ecb.AddComponent(barracksEntity, new BuildingCostTag
            {
                buildingType = BuildingType.BARRACKS
            });
            var barracksCosts = ecb.AddBuffer<ResourceHolder>(barracksEntity);
            barracksCosts.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 100
            });
            barracksCosts.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 100
            });

            var townHallEntity = ecb.CreateEntity();
            ecb.AddComponent(townHallEntity, new BuildingCostTag
            {
                buildingType = BuildingType.TOWN_HALL
            });
            var townHallCosts = ecb.AddBuffer<ResourceHolder>(townHallEntity);
            townHallCosts.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 1000
            });
            townHallCosts.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 1100
            });
            townHallCosts.Add(new ResourceHolder
            {
                type = ResourceType.GOLD,
                value = 1200
            });
            townHallCosts.Add(new ResourceHolder
            {
                type = ResourceType.FOOD,
                value = 1300
            });
        }
    }
}