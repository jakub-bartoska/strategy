using _Monobehaviors.ui.player_resources;
using component;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.strategy.army_components;
using component.strategy.buildings;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class TownSpawner
    {
        public static void spawnTown(float3 position, Team team, PrefabHolder prefabHolder, EntityCommandBuffer ecb,
            RefRW<IdGenerator> idGenerator, NativeList<ArmyCompany> companies)
        {
            var newEntity = ecb.Instantiate(prefabHolder.townPrefab);
            var transform = LocalTransform.FromPosition(position);

            var idHolder = new IdHolder
            {
                id = idGenerator.ValueRW.nextIdToBeUsed++,
                type = HolderType.TOWN
            };

            var teamComponent = new TeamComponent
            {
                team = team
            };
            var soldierSpawner = new SoldierSpawner
            {
                soldiersAmountToSpawn = 100,
                cycleTime = 3,
                timeLeft = 3,
                soldierType = SoldierType.SWORDSMAN
            };

            var townTeamMarker = SpawnUtils.spawnTeamMarker(ecb, teamComponent, newEntity, prefabHolder);
            teamComponent.teamMarker = townTeamMarker;

            ecb.SetName(newEntity, "Town " + idHolder.id);

            //add component
            ecb.AddComponent(newEntity, idHolder);
            ecb.AddComponent(newEntity, teamComponent);
            ecb.AddComponent(newEntity, soldierSpawner);
            ecb.AddComponent(newEntity, new TownTag());
            ecb.AddComponent(newEntity, new MarkableEntity());
            ecb.AddComponent(newEntity, new StrategyCleanupTag());

            //set component
            ecb.SetComponent(newEntity, transform);

            ecb.AddBuffer<Building>(newEntity);

            //add buffer
            var resourceHolders = ecb.AddBuffer<ResourceHolder>(newEntity);
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.GOLD,
                value = 1000
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 900
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 800
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.FOOD,
                value = 700
            });
            var companyBuffer = ecb.AddBuffer<ArmyCompany>(newEntity);
            companyBuffer.AddRange(companies.AsArray());

            var resourceGenerator = ecb.AddBuffer<ResourceGenerator>(newEntity);
            resourceGenerator.Add(new ResourceGenerator
            {
                type = ResourceType.GOLD,
                value = 10,
                defaultTimer = 3,
                timeRemaining = 3
            });

            spawnTownDeployer(ecb, idGenerator, transform, teamComponent);
        }

        private static void spawnTownDeployer(EntityCommandBuffer ecb, RefRW<IdGenerator> idGenerator,
            LocalTransform transform, TeamComponent team)
        {
            var newEntity = ecb.CreateEntity();

            var idHolder = new IdHolder
            {
                id = idGenerator.ValueRW.nextIdToBeUsed++,
                type = HolderType.TOWN_DEPLOYER
            };

            ecb.SetName(newEntity, "Deployer " + idHolder.id);

            ecb.AddComponent(newEntity, transform);
            ecb.AddComponent(newEntity, team);
            ecb.AddComponent(newEntity, idHolder);
            ecb.AddComponent(newEntity, new TownDeployerTag());
            ecb.AddComponent(newEntity, new MarkableEntity());

            ecb.AddBuffer<ArmyCompany>(newEntity);
        }
    }
}