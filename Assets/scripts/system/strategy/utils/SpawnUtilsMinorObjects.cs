using component;
using component.authoring_pairs.PrefabHolder;
using component.strategy.general;
using component.strategy.minor_objects;
using component.strategy.player_resources;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class SpawnUtilsMinorObjects
    {
        public static void spawnMinor(Team team, float3 position, HolderType type,
            NativeList<SpawnResourceGenerator> resourceGenerators,
            EntityCommandBuffer ecb, PrefabHolder prefabHolder, RefRW<IdGenerator> idGenerator)
        {
            var prefab = prefabHolder.millPrefab;
            var newEntity = ecb.Instantiate(prefab);
            var transform = LocalTransform.FromPosition(position);

            var idHolder = new IdHolder()
            {
                id = idGenerator.ValueRW.nextIdToBeUsed++,
                type = type
            };
            var teamComponent = new TeamComponent
            {
                team = team
            };
            var townTeamMarker = SpawnUtils.spawnTeamMarker(ecb, teamComponent, newEntity, prefabHolder);
            teamComponent.teamMarker = townTeamMarker;

            ecb.SetName(newEntity, "Mill " + idHolder.id);

            ecb.AddComponent(newEntity, transform);
            ecb.AddComponent(newEntity, idHolder);
            ecb.AddComponent(newEntity, teamComponent);

            ecb.AddBuffer<ResourceHolder>(newEntity);
            var resourceBuffer = ecb.AddBuffer<ResourceGenerator>(newEntity);
            foreach (var spawnResourceGenerator in resourceGenerators)
            {
                resourceBuffer.Add(new ResourceGenerator
                {
                    type = spawnResourceGenerator.type,
                    value = spawnResourceGenerator.value,
                    timeRemaining = 5,
                    defaultTimer = 5
                });
            }
        }
    }
}