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
            var result = SystemAPI.GetSingletonRW<BackupPlanDataHolder>().ValueRW.battleChunks;
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
                        result.Add(teamRow, chunkToSave);

                        chunkBattalions = new NativeList<long>(100, Allocator.Persistent);
                        chunkBattalions.Add(battalionInfo.battalionId);
                        currentChunk = new BattleChunk
                        {
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
                        rowId = currentChunk.Value.rowId,
                        leftFighting = currentChunk.Value.leftFighting,
                        rightFighting = false,
                        battalions = chunkBattalions,
                        startX = currentChunk.Value.startX,
                        endX = lastBattalion.Value.position.x + lastBattalion.Value.width / 2,
                        team = currentChunk.Value.team
                    };
                    result.Add(teamRowFinish, chunkToSave);
                }
            }
        }
    }
}