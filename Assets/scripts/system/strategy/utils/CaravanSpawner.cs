﻿using component;
using component.authoring_pairs.PrefabHolder;
using component.strategy.caravan;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class CaravanSpawner
    {
        public static void spawnCaravan(
            PrefabHolder prefabHolder,
            EntityCommandBuffer ecb,
            float3 position,
            Team team,
            NativeList<ResourceHolder> resourceHolder,
            RefRW<IdGenerator> idGenerator
        )
        {
            var newEntity = ecb.Instantiate(prefabHolder.caravanPrefab);
            var newTransform = LocalTransform.FromPosition(position);
            newTransform.Scale = 0.2f;
            var initSettings = new InitCaravanSetting();
            var markableEntity = new MarkableEntity();
            var teamComponent = new TeamComponent
            {
                team = team
            };
            var idHolder = new IdHolder()
            {
                id = idGenerator.ValueRW.nextIdToBeUsed++,
                type = HolderType.CARAVAN
            };

            var townTeamMarker = SpawnUtils.spawnTeamMarker(ecb, teamComponent, newEntity, prefabHolder);
            teamComponent.teamMarker = townTeamMarker;

            ecb.AddComponent(newEntity, initSettings);
            ecb.AddComponent(newEntity, teamComponent);
            ecb.AddComponent(newEntity, idHolder);
            ecb.AddComponent(newEntity, markableEntity);

            ecb.SetComponent(newEntity, newTransform);

            var resourceBuffer = ecb.AddBuffer<ResourceHolder>(newEntity);
            resourceBuffer.AddRange(resourceHolder.AsArray());
        }
    }
}