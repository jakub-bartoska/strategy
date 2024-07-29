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
    public partial struct CHM2_AddEmptyChunks : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var chunkHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();
            var battleChunks = chunkHolder.ValueRO.battleChunks;
            var result = chunkHolder.ValueRW.emptyChunks;

            var dataHolder = SystemAPI.GetSingleton<DataHolder>();
            var allRows = dataHolder.allRowIds;

            var team = Team.TEAM1;

            foreach (var rowId in allRows)
            {
                addChunkToEmptyRow(team, rowId, ref result, battleChunks);
            }
        }

        private void addChunkToEmptyRow(Team myTeam, int rowId, ref NativeParallelMultiHashMap<TeamRow, BattleChunk> result, NativeParallelMultiHashMap<TeamRow, BattleChunk> filledChunks)
        {
            var enemyKey = new TeamRow
            {
                rowId = rowId,
                team = getEDnemyTeam(myTeam)
            };
            var hasEnemyChunk = filledChunks.ContainsKey(enemyKey);

            var myKey = new TeamRow
            {
                rowId = rowId,
                team = myTeam
            };

            if (!hasEnemyChunk)
            {
                result.Add(myKey, new BattleChunk
                {
                    rowId = rowId,
                    leftFighting = false,
                    rightFighting = false,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize,
                    endX = CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize,
                    team = myTeam
                });
                return;
            }

            addChunk(myTeam, rowId, ref result, filledChunks);
        }

        private void addChunk(Team myTeam, int rowId, ref NativeParallelMultiHashMap<TeamRow, BattleChunk> result, NativeParallelMultiHashMap<TeamRow, BattleChunk> filledChunks)
        {
            var myKey = new TeamRow
            {
                rowId = rowId,
                team = myTeam
            };

            var enemyKey = new TeamRow
            {
                rowId = rowId,
                team = getEDnemyTeam(myTeam)
            };
            var enemyValues = filledChunks.GetValuesForKey(enemyKey);
            float? minX = null;
            float? maxX = null;
            foreach (var battleChunk in enemyValues)
            {
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
                result.Add(myKey, new BattleChunk
                {
                    rowId = rowId,
                    leftFighting = false,
                    rightFighting = true,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize,
                    endX = minX.Value,
                    team = myTeam
                });
            }

            if (maxX.HasValue)
            {
                result.Add(myKey, new BattleChunk
                {
                    rowId = rowId,
                    leftFighting = true,
                    rightFighting = false,
                    battalions = new NativeList<long>(0, Allocator.Persistent),
                    startX = maxX.Value,
                    endX = CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize,
                    team = myTeam
                });
            }
        }

        private Team getEDnemyTeam(Team myTeam)
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