using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.config.game_settings;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class BattalionSpawner
    {
        public static Entity spawnBattalion(EntityCommandBuffer ecb, BattalionToSpawn battalionToSpawn, PrefabHolder prefabHolder, long battalionId,
            float3 battalionPosition)
        {
            var battalionPrefab = prefabHolder.battalionPrefab;
            var newBattalion = ecb.Instantiate(battalionPrefab);

            battalionPosition.y = 0.02f;
            battalionPosition.z += 5f;
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);

            var battalionMarker = new BattalionMarker
            {
                id = battalionId,
                team = battalionToSpawn.team,
                row = battalionToSpawn.position.y
            };

            ecb.AddComponent(newBattalion, battalionMarker);

            ecb.AddBuffer<BattalionFightBuffer>(newBattalion);

            ecb.SetComponent(newBattalion, battalionTransform);

            return newBattalion;
        }
    }
}