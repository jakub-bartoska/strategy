using System;
using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.flank
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    public partial struct F1_FindFlankPositions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            //row - (battalionId, position, team, width)
            var positions = dataHolder.ValueRO.positions;
            //rowId - (team1FlankPosition, team2FlankPosition)
            var flankPositions = movementDataHolder.ValueRW.flankPositions;
            var allRowIds = dataHolder.ValueRO.allRowIds;

            foreach (var rowId in allRowIds)
            {
                float3? team1FlankPosition = null;
                float3? team2FlankPosition = null;
                foreach (var battalionInfo in positions.GetValuesForKey(rowId))
                {
                    switch (battalionInfo.team)
                    {
                        case Team.TEAM1:
                            team2FlankPosition = battalionInfo.position;
                            break;
                        case Team.TEAM2:
                            if (!team1FlankPosition.HasValue)
                            {
                                team1FlankPosition = battalionInfo.position;
                            }

                            break;
                        default:
                            throw new Exception("unknown team");
                    }
                }

                if (team1FlankPosition.HasValue || team2FlankPosition.HasValue)
                {
                    flankPositions.Add(rowId, (team1FlankPosition, team2FlankPosition));
                }
            }
        }
    }
}