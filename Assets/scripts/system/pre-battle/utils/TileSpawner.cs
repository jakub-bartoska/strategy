using component.authoring_pairs.PrefabHolder;
using component.pre_battle;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.battle.utils.pre_battle
{
    public class TileSpawner
    {
        public static void spawnTiles(PrefabHolder prefabHolder, EntityCommandBuffer ecb)
        {
            //iterate over rows
            for (int i = 0; i < 100; i++)
            {
                //iterate over columns
                for (int j = 0; j < 10; j++)
                {
                    spawnTile(new float2(i, j), prefabHolder, ecb);
                }
            }
        }

        private static void spawnTile(float2 position, PrefabHolder prefabHolder, EntityCommandBuffer ecb)
        {
            var tile = prefabHolder.archerTilePrefab;
            var newInstance = ecb.Instantiate(tile);
            var offset = CustomTransformUtils.defaulBattleMapOffset;

            var adjustedPosition = new float3
            {
                x = position.x / 4 + offset.x,
                y = 0 + offset.y,
                z = position.y + offset.z
            };

            var tileMarker = new TileMarker();

            var transform = LocalTransform.FromPosition(adjustedPosition);
            transform.Rotation = quaternion.EulerXYZ(
                90 * Mathf.PI / 180,
                0,
                //90 * Mathf.PI / 180,
                0
            );

            ecb.AddComponent(newInstance, tileMarker);
            ecb.SetComponent(newInstance, transform);
        }
    }
}