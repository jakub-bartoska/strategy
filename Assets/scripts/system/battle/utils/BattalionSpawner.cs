using System;
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
        public static Entity spawnBattalion(EntityCommandBuffer ecb, BattalionToSpawn battalionToSpawn, PrefabHolder prefabHolder, long battalionId)
        {
            var battalionPrefab = prefabHolder.battalionPrefab;
            var newBattalion = ecb.Instantiate(battalionPrefab);

            var battalionTransform = CustomTransformUtils.getBattalionPosition(battalionToSpawn.position.x, battalionToSpawn.position.y);
            var battalionMarker = new BattalionMarker
            {
                id = battalionId,
                soldierType = battalionToSpawn.armyType
            };
            var row = new Row
            {
                value = battalionToSpawn.position.y
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
                direction = direction
            };
            var size = getSizeForBattalionType(battalionToSpawn.armyType);
            var battalionSize = new BattalionWidth
            {
                value = size
            };
            var transformMatrix = getPostTransformMatrixFromBattalionSize(size);

            var possibleSplits = new PossibleSplit
            {
                up = false,
                down = false,
                left = false,
                right = false
            };

            ecb.AddComponent(newBattalion, battalionMarker);
            ecb.AddComponent(newBattalion, possibleSplits);
            ecb.AddComponent(newBattalion, movementDirection);
            ecb.AddComponent(newBattalion, row);
            ecb.AddComponent(newBattalion, team);
            ecb.AddComponent(newBattalion, battalionSize);
            ecb.AddComponent(newBattalion, transformMatrix);

            ecb.AddBuffer<BattalionFightBuffer>(newBattalion);

            ecb.SetComponent(newBattalion, battalionTransform);

            return newBattalion;
        }

        public static Entity spawnBattalionParallel(
            EntityCommandBuffer.ParallelWriter ecb,
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
            var newBattalion = ecb.Instantiate(0, battalionPrefab);
            battalionPosition.y = 0.02f;
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);
            var battalionMarker = new BattalionMarker
            {
                id = battalionId,
                soldierType = soldierType
            };
            var rowComponent = new Row
            {
                value = row
            };
            var teamComponent = new BattalionTeam
            {
                value = team
            };
            var possibleSplits = new PossibleSplit
            {
                up = false,
                down = false,
                left = false,
                right = false
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
                direction = direction
            };

            ecb.AddComponent(0, newBattalion, battalionMarker);
            ecb.AddComponent(0, newBattalion, possibleSplits);
            ecb.AddComponent(0, newBattalion, new WaitForSoldiers());
            ecb.AddComponent(0, newBattalion, movementDirection);
            ecb.AddComponent(0, newBattalion, rowComponent);
            ecb.AddComponent(0, newBattalion, teamComponent);
            ecb.AddComponent(0, newBattalion, battalionSize);
            ecb.AddComponent(0, newBattalion, transformMatrix);

            ecb.AddBuffer<BattalionFightBuffer>(0, newBattalion);
            var soldierBuffer = ecb.AddBuffer<BattalionSoldiers>(0, newBattalion);
            soldierBuffer.AddRange(soldiers.AsArray());

            ecb.SetComponent(0, newBattalion, battalionTransform);

            addAdditionalComponents(newBattalion, ecb);

            return newBattalion;
        }

        private static void addAdditionalComponents(Entity entity, EntityCommandBuffer.ParallelWriter ecb)
        {
            ecb.AddComponent(0, entity, new WaitForSoldiers());
        }

        private static float getSizeForBattalionType(SoldierType soldierType)
        {
            var scaleCoefficient = 0.3f;
            switch (soldierType)
            {
                case SoldierType.SWORDSMAN:
                    return 10f * scaleCoefficient;
                case SoldierType.ARCHER:
                    return 20f * scaleCoefficient;
                case SoldierType.HORSEMAN:
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