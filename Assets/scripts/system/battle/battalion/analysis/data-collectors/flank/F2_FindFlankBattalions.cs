using System;
using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.flank
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    [UpdateAfter(typeof(F1_FindFlankPositions))]
    public partial struct F2_FindFlankBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var positions = dataHolder.ValueRO.positions;
            //rowId - (team1FlankPosition, team2FlankPosition)
            var flankPositions = movementDataHolder.ValueRO.flankPositions;
            var flankingBattalions = dataHolder.ValueRW.flankingBattalions;
            var allRowIds = dataHolder.ValueRO.allRowIds;

            foreach (var rowId in allRowIds)
            {
                flankPositions.TryGetValue(rowId, out var teamFlanks);
                foreach (var battalionInfo in positions.GetValuesForKey(rowId))
                {
                    var flankPosition = battalionInfo.team switch
                    {
                        Team.TEAM1 => teamFlanks.team1,
                        Team.TEAM2 => teamFlanks.team2,
                        _ => throw new Exception("unknown team"),
                    };
                    if (!flankPosition.HasValue)
                    {
                        continue;
                    }

                    switch (battalionInfo.team)
                    {
                        case Team.TEAM1:
                            if (battalionInfo.position.x < flankPosition.Value.x)
                            {
                                flankingBattalions.Add(battalionInfo.battalionId);
                            }

                            break;
                        case Team.TEAM2:
                            if (battalionInfo.position.x > flankPosition.Value.x)
                            {
                                flankingBattalions.Add(battalionInfo.battalionId);
                            }

                            break;
                        default: throw new Exception("unknown team");
                    }
                }
            }
        }
    }
}