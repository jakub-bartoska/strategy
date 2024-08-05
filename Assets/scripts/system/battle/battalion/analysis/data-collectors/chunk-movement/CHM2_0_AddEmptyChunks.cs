using System;
using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM1_ParseToChunks))]
    public partial struct CHM2_0_AddEmptyChunks : ISystem
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
            var battleChunks = backupPlanDataHolder.ValueRO.battleChunksPerRowTeam;
            var emptyChunks = backupPlanDataHolder.ValueRW.emptyChunks;
            var allChunks = backupPlanDataHolder.ValueRW.allChunks;
            var lastChunkId = backupPlanDataHolder.ValueRW.lastChunkId;

            var dataHolder = SystemAPI.GetSingleton<DataHolder>();
            var allRows = dataHolder.allRowIds;

            foreach (var rowId in allRows)
            {
                addChunkToEmptyRow(Team.TEAM1, rowId, ref emptyChunks, battleChunks, allChunks, ref lastChunkId);
                addChunkToEmptyRow(Team.TEAM2, rowId, ref emptyChunks, battleChunks, allChunks, ref lastChunkId);
            }

            backupPlanDataHolder.ValueRW.lastChunkId = lastChunkId;
        }

        private void addChunkToEmptyRow(Team myTeam,
            int rowId,
            ref NativeParallelMultiHashMap<TeamRow, long> emptyChunks,
            NativeParallelMultiHashMap<TeamRow, long> filledChunks,
            NativeHashMap<long, BattleChunk> allChunks,
            ref long lastChunkId)
        {
            var enemyKey = new TeamRow
            {
                rowId = rowId,
                team = getEnemyTeam(myTeam)
            };
            var hasEnemyChunk = filledChunks.ContainsKey(enemyKey);

            var myKey = new TeamRow
            {
                rowId = rowId,
                team = myTeam
            };

            if (!hasEnemyChunk)
            {
                var hasMateInRow = filledChunks.ContainsKey(enemyKey);
                if (hasMateInRow)
                {
                    return;
                }

                var chunk = new BattleChunk
                {
                    chunkId = lastChunkId++,
                    rowId = rowId,
                    leftFighting = false,
                    rightFighting = false,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize,
                    endX = CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize,
                    team = myTeam
                };
                allChunks.Add(chunk.chunkId, chunk);
                emptyChunks.Add(myKey, chunk.chunkId);
                return;
            }

            addChunk(myTeam, rowId, ref emptyChunks, filledChunks, allChunks, ref lastChunkId);
        }

        private void addChunk(Team myTeam,
            int rowId,
            ref NativeParallelMultiHashMap<TeamRow, long> emptyChunks,
            NativeParallelMultiHashMap<TeamRow, long> filledChunks,
            NativeHashMap<long, BattleChunk> allChunks,
            ref long lastChunkId)
        {
            var myKey = new TeamRow
            {
                rowId = rowId,
                team = myTeam
            };

            var enemyKey = new TeamRow
            {
                rowId = rowId,
                team = getEnemyTeam(myTeam)
            };
            var enemyValues = filledChunks.GetValuesForKey(enemyKey);
            float? minX = null;
            float? maxX = null;
            foreach (var chunkId in enemyValues)
            {
                var battleChunk = allChunks[chunkId];
                var startX = battleChunk.startX;
                var endX = battleChunk.endX;
                if (!battleChunk.leftFighting)
                {
                    minX = startX;
                }

                if (!battleChunk.rightFighting)
                {
                    maxX = endX;
                }
            }

            if (minX.HasValue)
            {
                var chunk = new BattleChunk
                {
                    chunkId = lastChunkId++,
                    rowId = rowId,
                    leftFighting = false,
                    rightFighting = true,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize,
                    endX = minX.Value,
                    team = myTeam
                };
                allChunks.Add(chunk.chunkId, chunk);
                emptyChunks.Add(myKey, chunk.chunkId);
            }

            if (maxX.HasValue)
            {
                var chunk = new BattleChunk
                {
                    chunkId = lastChunkId++,
                    rowId = rowId,
                    leftFighting = true,
                    rightFighting = false,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = maxX.Value,
                    endX = CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize,
                    team = myTeam
                };
                allChunks.Add(chunk.chunkId, chunk);
                emptyChunks.Add(myKey, chunk.chunkId);
            }
        }

        private Team getEnemyTeam(Team myTeam)
        {
            return myTeam switch
            {
                Team.TEAM1 => Team.TEAM2,
                Team.TEAM2 => Team.TEAM1,
                _ => throw new Exception("Unknown team")
            };
        }
    }
}