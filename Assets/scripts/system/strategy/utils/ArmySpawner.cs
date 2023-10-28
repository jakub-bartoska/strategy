using _Monobehaviors.ui.player_resources;
using component;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.ui;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class ArmySpawner
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
    }
}