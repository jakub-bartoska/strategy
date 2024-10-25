using component;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.marker;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.battle.utils.pre_battle
{
    public class TileSpawner
    {
        public static void spawnTiles(PrefabHolder prefabHolder, EntityCommandBuffer ecb, DynamicBuffer<PreBattleBattalion> battalionBuffer)
        {
            var rowCount = 100;
            var columnCount = 16;
            //iterate over rows
            for (int i = -(rowCount / 2); i < rowCount / 2; i++)
            {
                //iterate over columns
                for (int j = -(columnCount / 2); j < columnCount / 2; j++)
                {
                    spawnTile(new float2(i, j), prefabHolder, ecb, battalionBuffer);
                }
            }
        }

        private static void spawnTile(float2 position, PrefabHolder prefabHolder, EntityCommandBuffer ecb, DynamicBuffer<PreBattleBattalion> battalionBuffer)
        {
            var offset = CustomTransformUtils.defaulBattleMapOffset;
            var adjustedPosition = new float3
            {
                x = position.x / 4 + offset.x,
                y = 0 + offset.y,
                z = position.y + offset.z
            };

            var entity = spawnTile(adjustedPosition, prefabHolder, ecb, null, null);

            battalionBuffer.Add(new PreBattleBattalion
            {
                position = adjustedPosition,
                entity = entity
            });
        }

        public static Entity spawnTile(float3 position, PrefabHolder prefabHolder, EntityCommandBuffer ecb, Team? team, SoldierType? soldierType)
        {
            var prefab = getProperPrefab(team, soldierType, prefabHolder);
            var newInstance = ecb.Instantiate(prefab);

            var tileMarker = new TileMarker();

            var transform = LocalTransform.FromPosition(position);
            transform.Rotation = quaternion.EulerXYZ(
                90 * Mathf.PI / 180,
                0,
                0
            );

            ecb.AddComponent(newInstance, tileMarker);
            ecb.SetComponent(newInstance, transform);

            return newInstance;
        }

        private static Entity getProperPrefab(Team? team, SoldierType? soldierType, PrefabHolder prefabHolder)
        {
            //todo spravny prefaby
            if (!team.HasValue)
            {
                return prefabHolder.archerTilePrefab;
            }

            return prefabHolder.archerTileFilledPrefab;
        }
    }
}