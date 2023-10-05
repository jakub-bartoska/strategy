using System;
using _Monobehaviors.ui.player_resources;
using component;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.general;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using component.strategy.ui;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class SpawnUtils
    {
        public static void spawnArmy(Team team, float3 position, NativeList<ArmyCompany> companies,
            EntityCommandBuffer ecb, PrefabHolder prefabHolder, RefRW<IdGenerator> idGenerator,
            DynamicBuffer<TeamColor> teamColors)
        {
            var prefab = team == Team.TEAM1 ? prefabHolder.armyPrefabTeam1 : prefabHolder.armyPrefabTeam2;
            var newEntity = ecb.Instantiate(prefab);
            var transform = LocalTransform.FromPosition(position);

            var idHolder = new IdHolder
            {
                id = idGenerator.ValueRW.nextIdToBeUsed++,
                type = HolderType.ARMY
            };

            var teamComponent = new TeamComponent
            {
                team = team
            };

            var color = new MaterialColorComponent
            {
                Value = getColor(team, teamColors)
            };

            var soldierCount = 0;
            foreach (var armyCompany in companies)
            {
                soldierCount += armyCompany.soldierCount;
            }

            var uiLabel = new StrategyUiLabel
            {
                id = idGenerator.ValueRW.nextIdToBeUsed - 1,
                text = soldierCount.ToString(),
                position = position
            };

            var armyMovementStatus = new ArmyMovementStatus
            {
                movementType = MovementType.IDLE
            };

            ecb.SetName(newEntity, "Army " + idHolder.id);

            //add component
            ecb.AddComponent(newEntity, color);
            ecb.AddComponent(newEntity, idHolder);
            ecb.AddComponent(newEntity, uiLabel);
            ecb.AddComponent(newEntity, armyMovementStatus);
            ecb.AddComponent(newEntity, teamComponent);
            ecb.AddComponent(newEntity, new ArmyMovement());
            ecb.AddComponent(newEntity, new ArmyTag());
            ecb.AddComponent(newEntity, new MarkableEntity());
            ecb.AddComponent(newEntity, new StrategyCleanupTag());

            //set component
            ecb.SetComponent(newEntity, transform);

            //add buffer
            ecb.AddBuffer<ArmyInteraction>(newEntity);
            var companyBuffer = ecb.AddBuffer<ArmyCompany>(newEntity);
            companyBuffer.AddRange(companies.AsArray());
            var resources = ecb.AddBuffer<ResourceHolder>(newEntity);
            resources.Add(new ResourceHolder
            {
                value = 100,
                type = ResourceType.GOLD
            });
            resources.Add(new ResourceHolder
            {
                value = 150,
                type = ResourceType.FOOD
            });
            resources.Add(new ResourceHolder
            {
                value = 200,
                type = ResourceType.WOOD
            });
        }

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

            var townTeamMarker = spawnTeamMarker(ecb, teamComponent, newEntity, prefabHolder);
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

            //add buffer
            var resourceHolders = ecb.AddBuffer<ResourceHolder>(newEntity);
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.GOLD,
                value = 100
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.WOOD,
                value = 90
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.STONE,
                value = 80
            });
            resourceHolders.Add(new ResourceHolder
            {
                type = ResourceType.FOOD,
                value = 70
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

        public static Entity spawnTeamMarker(EntityCommandBuffer ecb, TeamComponent team, Entity townEntity, PrefabHolder prefabHolder)
        {
            var prefab = team.team == Team.TEAM1
                ? prefabHolder.townTeamMarkerTeam1Prefab
                : prefabHolder.townTeamMarkerTeam2Prefab;
            var townTeamMarker = ecb.Instantiate(prefab);
            ecb.SetName(townTeamMarker, "Town team marker");
            ecb.AddComponent(townTeamMarker, new Parent
            {
                Value = townEntity
            });
            return townTeamMarker;
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

        private static float4 getColor(Team team, DynamicBuffer<TeamColor> colors)
        {
            foreach (var color in colors)
            {
                if (color.team == team)
                    return color.color;
            }

            throw new Exception("unknown team");
        }
    }
}