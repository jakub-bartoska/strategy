using System;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.row_change
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(RC1_GetRowChangeDirections))]
    public partial struct RC2_MarkRowChangeBattalions : ISystem
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

            var positions = dataHolder.ValueRO.positions;
            var allRowIds = dataHolder.ValueRO.allRowIds;
            var rowChanges = dataHolder.ValueRO.rowChanges;
            var result = dataHolder.ValueRW.battalionSwitchRowDirections;

            foreach (var rowId in allRowIds)
            {
                var rowChangesPerTeam = rowChanges[rowId];
                var team1Direction = rowChangesPerTeam.team1.direction;
                var team1Position = getFlankingPositionForRow(rowChangesPerTeam.team1.closestEnemyRow, Team.TEAM1, movementDataHolder.ValueRO);

                var team2Direction = rowChangesPerTeam.team2.direction;
                var team2Position = getFlankingPositionForRow(rowChangesPerTeam.team2.closestEnemyRow, Team.TEAM2, movementDataHolder.ValueRO);

                foreach (var battalionInfo in positions.GetValuesForKey(rowId))
                {
                    if (battalionInfo.unitType == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    if (battalionInfo.team == Team.TEAM1)
                    {
                        //battalion position + battalion width < flank position
                        if (battalionInfo.position.x + battalionInfo.width * 1.1f < team1Position.Value.x)
                        {
                            result.Add(battalionInfo.battalionId, team1Direction);
                        }
                    }

                    if (battalionInfo.team == Team.TEAM2)
                    {
                        //battalion position - battalion width > flank position
                        if (battalionInfo.position.x - battalionInfo.width * 1.1f > team2Position.Value.x)
                        {
                            result.Add(battalionInfo.battalionId, team2Direction);
                        }
                    }
                }
            }
        }

        private float3? getFlankingPositionForRow(int targetRow, Team team, MovementDataHolder movementDataHolder)
        {
            movementDataHolder.flankPositions.TryGetValue(targetRow, out var teamFlanks);
            return team switch
            {
                Team.TEAM1 => teamFlanks.team1,
                Team.TEAM2 => teamFlanks.team2,
                _ => throw new Exception("unknown team"),
            };
        }
    }
}