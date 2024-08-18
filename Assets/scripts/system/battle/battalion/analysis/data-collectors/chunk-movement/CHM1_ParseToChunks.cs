using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    [UpdateAfter(typeof(FindFightingPairsSystem))]
    public partial struct CHM1_ParseToChunks : ISystem
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
            var allChunks = backupPlanDataHolder.ValueRW.allChunks;
            var battleChunksPerRowTeam = backupPlanDataHolder.ValueRW.battleChunksPerRowTeam;
            var lastChunkId = backupPlanDataHolder.ValueRW.lastChunkId;

            var dataHolder = SystemAPI.GetSingleton<DataHolder>();

            var allRows = dataHolder.allRowIds;
            var positions = dataHolder.positions;

            foreach (var rowId in allRows)
            {
                var firstBatalion = true;
                BattleChunk? currentChunk = null;
                var chunkBattalions = new NativeList<long>(100, Allocator.Persistent);
                BattalionInfo? lastBattalion = null;
                foreach (var battalionInfo in positions.GetValuesForKey(rowId))
                {
                    if (battalionInfo.unitType == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    lastBattalion = battalionInfo;

                    if (!currentChunk.HasValue)
                    {
                        currentChunk = new BattleChunk
                        {
                            chunkId = lastChunkId++,
                            rowId = rowId,
                            leftFighting = !firstBatalion,
                            rightFighting = false,
                            battalions = chunkBattalions,
                            startX = battalionInfo.position.x - battalionInfo.width / 2,
                            endX = -1, //will be saved later 
                            team = battalionInfo.team
                        };
                    }

                    firstBatalion = false;

                    if (currentChunk.Value.team == battalionInfo.team)
                    {
                        chunkBattalions.Add(battalionInfo.battalionId);
                    }
                    else
                    {
                        //update immutable old value to save it and then delete it
                        var chunkToSave = new BattleChunk
                        {
                            chunkId = currentChunk.Value.chunkId,
                            rowId = currentChunk.Value.rowId,
                            leftFighting = currentChunk.Value.leftFighting,
                            rightFighting = true,
                            battalions = chunkBattalions,
                            startX = currentChunk.Value.startX,
                            endX = battalionInfo.position.x - battalionInfo.width / 2,
                            team = currentChunk.Value.team
                        };

                        var teamRow = new TeamRow
                        {
                            rowId = rowId,
                            team = currentChunk.Value.team
                        };
                        allChunks.Add(chunkToSave.chunkId, chunkToSave);
                        battleChunksPerRowTeam.Add(teamRow, chunkToSave.chunkId);

                        chunkBattalions = new NativeList<long>(100, Allocator.Persistent);
                        chunkBattalions.Add(battalionInfo.battalionId);
                        currentChunk = new BattleChunk
                        {
                            chunkId = lastChunkId++,
                            rowId = rowId,
                            leftFighting = !firstBatalion,
                            rightFighting = false,
                            battalions = chunkBattalions,
                            startX = battalionInfo.position.x - battalionInfo.width / 2,
                            endX = -1, //will be saved later
                            team = battalionInfo.team
                        };
                    }
                }

                //if chunk is the most right, there was not performed save into result, do it now
                if (currentChunk.HasValue)
                {
                    var teamRowFinish = new TeamRow
                    {
                        rowId = rowId,
                        team = currentChunk.Value.team
                    };
                    var chunkToSave = new BattleChunk
                    {
                        chunkId = currentChunk.Value.chunkId,
                        rowId = currentChunk.Value.rowId,
                        leftFighting = currentChunk.Value.leftFighting,
                        rightFighting = false,
                        battalions = chunkBattalions,
                        startX = currentChunk.Value.startX,
                        endX = lastBattalion.Value.position.x + lastBattalion.Value.width / 2,
                        team = currentChunk.Value.team
                    };
                    allChunks.Add(chunkToSave.chunkId, chunkToSave);
                    battleChunksPerRowTeam.Add(teamRowFinish, chunkToSave.chunkId);
                }
            }

            var battalionIdToChunk = backupPlanDataHolder.ValueRW.battalionIdToChunk;
            createBattalionToChunkMap(allChunks, battalionIdToChunk);
            backupPlanDataHolder.ValueRW.lastChunkId = lastChunkId;
        }

        private void createBattalionToChunkMap(NativeHashMap<long, BattleChunk> allChunks, NativeHashMap<long, long> result)
        {
            foreach (var chunkPair in allChunks)
            {
                var chunk = chunkPair.Value;
                foreach (var battalionId in chunk.battalions)
                {
                    result.Add(battalionId, chunk.chunkId);
                }
            }
        }
    }
}