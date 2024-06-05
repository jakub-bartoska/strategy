using System;
using component;
using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.flank
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
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
            var positions = DataHolder.positions;
            //rowId - (team1FlankPosition, team2FlankPosition)
            var flankPositions = MovementDataHolder.flankPositions;
            var flankingBattalions = DataHolder.flankingBattalions;
            var allRowIds = DataHolder.allRowIds;

            foreach (var rowId in allRowIds)
            {
                flankPositions.TryGetValue(rowId, out var teamFlanks);
                foreach (var battalionInfo in positions.GetValuesForKey(rowId))
                {
                    var flankPosition = battalionInfo.Item3 switch
                    {
                        Team.TEAM1 => teamFlanks.Item1,
                        Team.TEAM2 => teamFlanks.Item2,
                        _ => throw new Exception("unknown team"),
                    };
                    if (!flankPosition.HasValue)
                    {
                        continue;
                    }

                    switch (battalionInfo.Item3)
                    {
                        case Team.TEAM1:
                            if (battalionInfo.Item2.x < flankPosition.Value.x)
                            {
                                flankingBattalions.Add(battalionInfo.Item1);
                            }

                            break;
                        case Team.TEAM2:
                            if (battalionInfo.Item2.x > flankPosition.Value.x)
                            {
                                flankingBattalions.Add(battalionInfo.Item1);
                            }

                            break;
                        default: throw new Exception("unknown team");
                    }
                }
            }
        }
    }
}