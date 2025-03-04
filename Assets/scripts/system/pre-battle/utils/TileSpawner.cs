﻿using System;
using component;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.marker;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.battle.utils.pre_battle
{
    public class TileSpawner
    {
        public static NativeList<PreBattleBattalion> spawnTiles(PrefabHolder prefabHolder, EntityManager entityManager,
            NativeHashMap<float3, BattalionToSpawn> positionToBattalionMap)
        {
            Debug.Log(positionToBattalionMap.Count);
            //var rowCount = 200;
            var rowCount = 100;
            //var columnCount = 32;
            var columnCount = 100; 
            var result = new NativeList<PreBattleBattalion>(Allocator.Temp);
            //iterate over rows
            for (int i = -(rowCount / 2); i < rowCount / 2; i++)
            {
                //iterate over columns
                for (int j = -(columnCount / 2); j < columnCount / 2; j++)
                {
                    var newBattalionCard = spawnTile(new float2(i, j), prefabHolder, entityManager,
                        positionToBattalionMap);
                    result.Add(newBattalionCard);
                }
            }

            return result;
        }

        private static PreBattleBattalion spawnTile(float2 position, PrefabHolder prefabHolder,
            EntityManager entityManager, NativeHashMap<float3, BattalionToSpawn> positionToBattalionMap)
        {
            var offset = CustomTransformUtils.defaulBattleMapOffset;
            var adjustedPosition = new float3
            {
                x = position.x / 4 + offset.x,
                y = 0.02f + offset.y,
                z = position.y + offset.z
            };

            if (positionToBattalionMap.TryGetValue(adjustedPosition, out var battalionToSpawn))
            {
                var entity = spawnTile(adjustedPosition, prefabHolder, entityManager, battalionToSpawn.team,
                    battalionToSpawn.armyType);

                Debug.Log("Spawned battalion at " + adjustedPosition);

                return new PreBattleBattalion
                {
                    position = adjustedPosition,
                    entity = entity,
                    battalionId = battalionToSpawn.battalionId,
                    soldierType = battalionToSpawn.armyType,
                    team = battalionToSpawn.team
                };
            }
            else
            {
                var entity = spawnTile(adjustedPosition, prefabHolder, entityManager, null, null);

                return new PreBattleBattalion
                {
                    position = adjustedPosition,
                    entity = entity
                };
            }
        }

        public static Entity spawnTile(float3 position, PrefabHolder prefabHolder, EntityManager entityManager,
            Team? team, SoldierType? soldierType)
        {
            var prefab = getProperPrefab(team, soldierType, prefabHolder);
            var newInstance = entityManager.Instantiate(prefab);

            var tileMarker = new TileMarker();

            var transform = LocalTransform.FromPosition(position);
            transform.Rotation = quaternion.EulerXYZ(
                90 * Mathf.PI / 180,
                0,
                0
            );

            entityManager.AddComponentData(newInstance, new PreBattleCleanupTag());
            entityManager.AddComponentData(newInstance, tileMarker);
            entityManager.SetComponentData(newInstance, transform);

            return newInstance;
        }

        private static Entity getProperPrefab(Team? team, SoldierType? soldierType, PrefabHolder prefabHolder)
        {
            if (!team.HasValue)
            {
                return prefabHolder.emptyTilePrefab;
            }

            switch (team)
            {
                case Team.TEAM1:
                    switch (soldierType)
                    {
                        case SoldierType.ARCHER:
                            return prefabHolder.redArcherTilePrefab;
                        case SoldierType.SWORDSMAN:
                            return prefabHolder.redSwordsmanTilePrefab;
                        case SoldierType.CAVALRY:
                            return prefabHolder.redCavalryTilePrefab;
                        default:
                            throw new Exception("Unknown soldier type");
                    }
                case Team.TEAM2:
                    switch (soldierType)
                    {
                        case SoldierType.ARCHER:
                            return prefabHolder.blueArcherTilePrefab;
                        case SoldierType.SWORDSMAN:
                            return prefabHolder.blueSwordsmanTilePrefab;
                        case SoldierType.CAVALRY:
                            return prefabHolder.blueCavalryTilePrefab;
                        default:
                            throw new Exception("Unknown soldier type");
                    }
                default:
                    throw new Exception("Unknown team");
            }
        }

        public static Entity spawnTile(float3 position, PrefabHolder prefabHolder, EntityCommandBuffer ecb, Team? team,
            SoldierType? soldierType)
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

            ecb.AddComponent(newInstance, new PreBattleCleanupTag());
            ecb.AddComponent(newInstance, tileMarker);
            ecb.SetComponent(newInstance, transform);

            return newInstance;
        }
    }
}