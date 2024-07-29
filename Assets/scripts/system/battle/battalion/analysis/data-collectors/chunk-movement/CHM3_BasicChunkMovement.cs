using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM1_ParseToChunks))]
    public partial struct CHM3_BasicChunkMovement : ISystem
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
            var chunks = backupPlanDataHolder.ValueRW.battleChunks;

            //todo bacha, neni to zinicializovano
            var moveLeft = backupPlanDataHolder.ValueRW.moveLeft;
            var moveRight = backupPlanDataHolder.ValueRW.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.ValueRW.moveToDifferentChunk;

            var dataHolder = SystemAPI.GetSingleton<DataHolder>();
            var battalionInfo = dataHolder.battalionInfo;


            var descSorter = new SortByPositionDesc();
            foreach (var chunk in chunks.GetValueArray(Allocator.Temp))
            {
                var chunkBattalionCount = chunk.battalions.Length;
                if (chunkBattalionCount == 0)
                    continue;

                if (!chunk.leftFighting && !chunk.rightFighting)
                {
                    continue;
                }

                var battleInfo = new NativeList<BattalionInfo>(100, Allocator.Temp);
                foreach (var chunkBattalion in chunk.battalions)
                {
                    battleInfo.Add(battalionInfo[chunkBattalion]);
                }

                battleInfo.Sort(descSorter);

                var availableDirections = chunkToAvailableDirection(chunk);
                if (availableDirections == ChunkDirection.NONE)
                {
                    moveToDifferentChunk.AddRange(battleInfo);
                    continue;
                }

                var directionToStart = availableDirections == ChunkDirection.RIGHT ? ChunkDirection.RIGHT : ChunkDirection.LEFT;
                splitChunk(0, battleInfo, moveLeft, moveRight, moveToDifferentChunk, directionToStart, availableDirections);
            }
        }

        private ChunkDirection chunkToAvailableDirection(BattleChunk chunk)
        {
            if (chunk.leftFighting && chunk.rightFighting)
            {
                return ChunkDirection.BOTH;
            }

            if (!chunk.leftFighting && chunk.rightFighting)
            {
                return ChunkDirection.RIGHT;
            }

            if (chunk.leftFighting && !chunk.rightFighting)
            {
                return ChunkDirection.LEFT;
            }

            return ChunkDirection.NONE;
        }

        private void splitChunk(
            int battalionsSend,
            NativeList<BattalionInfo> orderedBattleInfo,
            NativeList<BattalionInfo> moveLeft,
            NativeList<BattalionInfo> moveRight,
            NativeList<BattalionInfo> moveToDifferentChunk,
            ChunkDirection direction,
            ChunkDirection availableDirections
        )
        {
            if (battalionsSend == orderedBattleInfo.Length)
                return;

            var isFull = availableDirections switch
            {
                ChunkDirection.LEFT => battalionsSend + 1 > 1,
                ChunkDirection.RIGHT => battalionsSend + 1 > 1,
                ChunkDirection.BOTH => battalionsSend + 1 > 2,
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
                moveToDifferentChunk.Add(battalionToPick);
            }
            else
            {
                if (direction == ChunkDirection.LEFT)
                {
                    moveLeft.Add(battalionToPick);
                }

                if (direction == ChunkDirection.RIGHT)
                {
                    moveRight.Add(battalionToPick);
                }
            }

            var nextDirection = pickNextDirection(direction, availableDirections);

            splitChunk(battalionsSend + 1, orderedBattleInfo, moveLeft, moveRight, moveToDifferentChunk, nextDirection, availableDirections);
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

        private enum ChunkDirection
        {
            LEFT,
            RIGHT,
            NONE,
            BOTH
        }
    }
}