﻿using System;
using component;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.markers;
using component.config.game_settings;
using system.battle.enums;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class BattalionSpawner
    {
        public static Entity spawnBattalion(EntityCommandBuffer ecb, BattalionToSpawn battalionToSpawn,
            PrefabHolder prefabHolder, long battalionId)
        {
            var battalionPrefab = prefabHolder.battalionPrefab;
            var newBattalion = ecb.Instantiate(battalionPrefab);

            var battalionTransform =
                CustomTransformUtils.getBattalionPosition(battalionToSpawn.position.Value);
            var battalionMarker = new BattalionMarker
            {
                id = battalionId,
                soldierType = battalionToSpawn.armyType
            };
            var battleUnitType = new BattleUnitType
            {
                id = battalionId,
                type = BattleUnitTypeEnum.BATTALION
            };
            var rowValue = CustomTransformUtils.positionToRow(battalionToSpawn.position.Value, 10);
            var row = new Row
            {
                value = rowValue
            };
            var team = new BattalionTeam
            {
                value = battalionToSpawn.team
            };
            var direction = battalionToSpawn.team switch
            {
                Team.TEAM1 => Direction.LEFT,
                Team.TEAM2 => Direction.RIGHT,
                _ => throw new Exception("Unknown team")
            };
            var movementDirection = new MovementDirection
            {
                defaultDirection = direction
            };
            var size = getSizeForBattalionType(battalionToSpawn.armyType);
            var battalionSize = new BattalionWidth
            {
                value = size
            };
            var soldierReorderMarker = new SoldierReorderMarker();
            var waitForSoldiers = new WaitForSoldiersTag();

            var transformMatrix = getPostTransformMatrixFromBattalionSize(size);

            ecb.AddComponent(newBattalion, battalionMarker);
            ecb.AddComponent(newBattalion, movementDirection);
            ecb.AddComponent(newBattalion, row);
            ecb.AddComponent(newBattalion, team);
            ecb.AddComponent(newBattalion, battleUnitType);
            ecb.AddComponent(newBattalion, battalionSize);
            ecb.AddComponent(newBattalion, transformMatrix);
            ecb.AddComponent(newBattalion, waitForSoldiers);
            ecb.AddComponent(newBattalion, soldierReorderMarker);
            ecb.SetComponentEnabled<SoldierReorderMarker>(newBattalion, true);

            ecb.SetComponent(newBattalion, battalionTransform);

            return newBattalion;
        }

        public static Entity spawnBattalionParallel(
            EntityCommandBuffer ecb,
            PrefabHolder prefabHolder,
            long battalionId,
            float3 battalionPosition,
            Team team,
            int row,
            NativeList<BattalionSoldiers> soldiers,
            SoldierType soldierType
        )
        {
            var battalionPrefab = prefabHolder.battalionPrefab;
            var newBattalion = ecb.Instantiate(battalionPrefab);
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);
            var battalionMarker = new BattalionMarker
            {
                id = battalionId,
                soldierType = soldierType
            };
            var battleUnitType = new BattleUnitType
            {
                id = battalionId,
                type = BattleUnitTypeEnum.BATTALION
            };
            var rowComponent = new Row
            {
                value = row
            };
            var teamComponent = new BattalionTeam
            {
                value = team
            };

            var size = getSizeForBattalionType(soldierType);
            var battalionSize = new BattalionWidth
            {
                value = size
            };
            var transformMatrix = getPostTransformMatrixFromBattalionSize(size);

            var direction = team switch
            {
                Team.TEAM1 => Direction.LEFT,
                Team.TEAM2 => Direction.RIGHT,
                _ => throw new Exception("Unknown team")
            };

            var movementDirection = new MovementDirection
            {
                defaultDirection = direction
            };

            var battalionHealth = new BattalionHealth
            {
                value = soldiers.Length * 10
            };
            var waitForSoldiers = new WaitForSoldiersTag();

            ecb.AddComponent(newBattalion, battalionHealth);
            ecb.AddComponent(newBattalion, battalionMarker);
            ecb.AddComponent(newBattalion, movementDirection);
            ecb.AddComponent(newBattalion, rowComponent);
            ecb.AddComponent(newBattalion, teamComponent);
            ecb.AddComponent(newBattalion, battalionSize);
            ecb.AddComponent(newBattalion, transformMatrix);
            ecb.AddComponent(newBattalion, battleUnitType);
            ecb.AddComponent(newBattalion, waitForSoldiers);

            var soldierBuffer = ecb.AddBuffer<BattalionSoldiers>(newBattalion);
            soldierBuffer.AddRange(soldiers.AsArray());

            ecb.SetComponent(newBattalion, battalionTransform);

            return newBattalion;
        }

        public static float getSizeForBattalionType(SoldierType soldierType)
        {
            var scaleCoefficient = 0.3f;
            switch (soldierType)
            {
                case SoldierType.SWORDSMAN:
                    return 10f * scaleCoefficient;
                case SoldierType.ARCHER:
                    return 20f * scaleCoefficient;
                case SoldierType.CAVALRY:
                    return 30f * scaleCoefficient;
                default:
                    throw new NotImplementedException("unknown type");
            }
        }

        public static PostTransformMatrix getPostTransformMatrixFromBattalionSize(float battalionSize)
        {
            var scaleCoefficient = 0.1f;
            var xScale = battalionSize * scaleCoefficient;

            return new PostTransformMatrix
            {
                Value = float4x4.Scale(new float3(xScale, 1, 1))
            };
        }
    }
}