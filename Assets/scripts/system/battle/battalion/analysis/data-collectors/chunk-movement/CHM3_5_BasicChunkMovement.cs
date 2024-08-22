using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_4_FindReinforcementPaths))]
    public partial struct CHM3_5_BasicChunkMovement : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var backupPlanDataHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();
            var battleChunksPerRowTeam = backupPlanDataHolder.ValueRW.battleChunksPerRowTeam;
            var allChunks = backupPlanDataHolder.ValueRW.allChunks;
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var allBattalions = dataHolder.ValueRO.battalionInfo;
            var chunkReinforcementPaths = backupPlanDataHolder.ValueRW.chunkReinforcementPaths;

            var moveLeft = backupPlanDataHolder.ValueRW.moveLeft;
            var moveRight = backupPlanDataHolder.ValueRW.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.ValueRW.moveToDifferentChunk;

            foreach (var chunkId in battleChunksPerRowTeam.GetValueArray(Allocator.Temp))
            {
                var chunk = allChunks[chunkId];
                var chunkBattalionCount = chunk.battalions.Length;
                if (chunkBattalionCount == 0)
                    continue;

                var battalions = new NativeList<BattalionInfo>(100, Allocator.Temp);
                foreach (var chunkBattalion in chunk.battalions)
                {
                    battalions.Add(allBattalions[chunkBattalion]);
                }

                battalions.Sort(new SortByPositionDesc());

                var availableDirections = chunkToAvailableDirection(chunk);
                if (availableDirections == ChunkDirection.NONE)
                {
                    foreach (var battalionInfo in battalions)
                    {
                        moveToDifferentChunk.Add(battalionInfo.battalionId);
                    }

                    continue;
                }

                var chunkPath = chunkReinforcementPaths[chunkId];
                var maxBattalionsPerSide = getMaxBattalionsPerSide(chunkPath.pathLength, chunkPath.targetChunkBattalionCount);
                var directionToStart = getDirectionToStart(chunk, availableDirections, allBattalions, allChunks);
                splitChunk(0, battalions, moveLeft, moveRight, moveToDifferentChunk, directionToStart, availableDirections, maxBattalionsPerSide);
            }
        }

        private ChunkDirection getDirectionToStart(BattleChunk chunk, ChunkDirection availableDirections, NativeHashMap<long, BattalionInfo> allBattalions, NativeHashMap<long, BattleChunk> allChunks)
        {
            if (availableDirections == ChunkDirection.BOTH && chunk.battalions.Length == 1)
            {
                var battalion = allBattalions[chunk.battalions[0]];
                var leftBorder = getRightBorderOfEnemyChunk(allBattalions, allChunks[chunk.leftEnemy.Value]);
                var distanceToLeft = math.abs(leftBorder - battalion.position.x);
                var distanceToRight = math.abs(chunk.endX - battalion.position.x);
                if (distanceToLeft < distanceToRight)
                {
                    return ChunkDirection.LEFT;
                }

                return ChunkDirection.RIGHT;
            }

            return availableDirections == ChunkDirection.RIGHT ? ChunkDirection.RIGHT : ChunkDirection.LEFT;
        }

        private float getRightBorderOfEnemyChunk(NativeHashMap<long, BattalionInfo> allBattalions, BattleChunk chunk)
        {
            float? mostRightX = null;
            foreach (var battalionId in chunk.battalions)
            {
                var battalion = allBattalions[battalionId];
                if (!mostRightX.HasValue || battalion.position.x > mostRightX)
                {
                    mostRightX = battalion.position.x;
                }
            }

            return mostRightX.Value;
        }

        private ChunkDirection chunkToAvailableDirection(BattleChunk chunk)
        {
            if (chunk.leftEnemy.HasValue && chunk.rightEnemy.HasValue)
            {
                return ChunkDirection.BOTH;
            }

            if (!chunk.leftEnemy.HasValue && chunk.rightEnemy.HasValue)
            {
                return ChunkDirection.RIGHT;
            }

            if (chunk.leftEnemy.HasValue && !chunk.rightEnemy.HasValue)
            {
                return ChunkDirection.LEFT;
            }

            return ChunkDirection.NONE;
        }

        private void splitChunk(
            int battalionsSend,
            NativeList<BattalionInfo> orderedBattleInfo,
            NativeList<long> moveLeft,
            NativeList<long> moveRight,
            NativeList<long> moveToDifferentChunk,
            ChunkDirection direction,
            ChunkDirection availableDirections,
            int maxBattalionsPerSide
        )
        {
            if (battalionsSend == orderedBattleInfo.Length)
                return;

            var isFull = availableDirections switch
            {
                ChunkDirection.LEFT => battalionsSend + 1 > maxBattalionsPerSide,
                ChunkDirection.RIGHT => battalionsSend + 1 > maxBattalionsPerSide,
                ChunkDirection.BOTH => battalionsSend + 1 > maxBattalionsPerSide * 2,
                ChunkDirection.NONE => true
            };

            //if enemies are on both sides, index is 1/2
            var index = availableDirections == direction ? battalionsSend : battalionsSend / 2;
            var battalionToPick = direction switch
            {
                ChunkDirection.LEFT => orderedBattleInfo[index],
                ChunkDirection.RIGHT => orderedBattleInfo[orderedBattleInfo.Length - 1 - index],
                _ => throw new Exception("Unexpected direction value: " + direction)
            };

            if (isFull)
            {
                moveToDifferentChunk.Add(battalionToPick.battalionId);
            }
            else
            {
                if (direction == ChunkDirection.LEFT)
                {
                    moveLeft.Add(battalionToPick.battalionId);
                }

                if (direction == ChunkDirection.RIGHT)
                {
                    moveRight.Add(battalionToPick.battalionId);
                }
            }

            var nextDirection = pickNextDirection(direction, availableDirections);

            splitChunk(battalionsSend + 1, orderedBattleInfo, moveLeft, moveRight, moveToDifferentChunk, nextDirection, availableDirections, maxBattalionsPerSide);
        }

        private ChunkDirection pickNextDirection(ChunkDirection current, ChunkDirection available)
        {
            if (current == ChunkDirection.NONE) throw new Exception("Unexpected direction value: " + current);

            if (current == available)
            {
                return current;
            }

            if (available == ChunkDirection.BOTH)
            {
                return current == ChunkDirection.LEFT ? ChunkDirection.RIGHT : ChunkDirection.LEFT;
            }

            throw new Exception("Unexpected direction value: " + current + " " + available);
        }

        private int getMaxBattalionsPerSide(int pathLength, int targetBattalionCount)
        {
            var coefficient = 1f / (1f + pathLength) / (1f + targetBattalionCount * 1.5f);
            return (int) ((1f / coefficient) - 1f);
        }

        private enum ChunkDirection
        {
            LEFT,
            RIGHT,
            NONE,
            BOTH
        }
    }
}