using System;
using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.flank;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.row_change
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(F2_FindFlankBattalions))]
    [UpdateAfter(typeof(FindBlockerSystem))]
    public partial struct RC1_GetRowChangeDirections : ISystem
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
            //rowId - flank direction (UP/DOWN), rowId to switch to
            var team1RowChanges = new NativeHashMap<int, (Direction, int)>(10, Allocator.Temp);
            var team2RowChanges = new NativeHashMap<int, (Direction, int)>(10, Allocator.Temp);
            setNoRowChanges(team1RowChanges, Team.TEAM1, dataHolder.ValueRO, movementDataHolder.ValueRO);
            setNoRowChanges(team2RowChanges, Team.TEAM2, dataHolder.ValueRO, movementDataHolder.ValueRO);
            fillClosestRows(team1RowChanges, dataHolder.ValueRO);
            fillClosestRows(team2RowChanges, dataHolder.ValueRO);

            fillRowChangesIntoDataHolder(team1RowChanges, team2RowChanges, dataHolder);
        }

        private void fillRowChangesIntoDataHolder(NativeHashMap<int, (Direction, int)> team1, NativeHashMap<int, (Direction, int)> team2, RefRW<DataHolder> dataHolder)
        {
            var rowChanges = dataHolder.ValueRW.rowChanges;
            var allRowIds = dataHolder.ValueRO.allRowIds;
            foreach (var rowId in allRowIds)
            {
                var team1Direction = team1[rowId];
                var team2Direction = team2[rowId];
                rowChanges.Add(rowId, new RowChange
                {
                    team1 = new TeamRowChange
                    {
                        direction = team1Direction.Item1,
                        closestEnemyRow = team1Direction.Item2
                    },
                    team2 = new TeamRowChange
                    {
                        direction = team2Direction.Item1,
                        closestEnemyRow = team2Direction.Item2
                    }
                });
            }
        }

        private void setNoRowChanges(NativeHashMap<int, (Direction, int)> tmpResult, Team team, DataHolder dataHolder, MovementDataHolder movementDataHolder)
        {
            var flankPositions = movementDataHolder.flankPositions;
            var allRowIds = dataHolder.allRowIds;

            foreach (var rowId in allRowIds)
            {
                float3? switchRow = null;
                if (flankPositions.TryGetValue(rowId, out var flankPositionsPerTeam))
                {
                    switchRow = team switch
                    {
                        Team.TEAM1 => flankPositionsPerTeam.team1,
                        Team.TEAM2 => flankPositionsPerTeam.team2,
                        _ => throw new Exception("unknown team")
                    };
                }

                if (switchRow.HasValue)
                {
                    tmpResult.Add(rowId, (Direction.NONE, rowId));
                }
            }
        }

        private void fillClosestRows(NativeHashMap<int, (Direction, int)> tmpResult, DataHolder dataHolder)
        {
            var allRowIds = dataHolder.allRowIds;
            foreach (var rowId in allRowIds)
            {
                if (tmpResult.ContainsKey(rowId))
                {
                    continue;
                }

                var closestUp = closestFilledRow(tmpResult, rowId, Direction.UP, 0);
                var closestDown = closestFilledRow(tmpResult, rowId, Direction.DOWN, 0);

                if (closestUp.Item1 == -1)
                {
                    tmpResult.Add(rowId, (Direction.DOWN, closestDown.Item2));
                    continue;
                }

                if (closestDown.Item1 == -1)
                {
                    tmpResult.Add(rowId, (Direction.UP, closestUp.Item2));
                    continue;
                }

                if (closestDown.Item1 > closestUp.Item1)
                {
                    tmpResult.Add(rowId, (Direction.UP, closestUp.Item2));
                }
                else
                {
                    tmpResult.Add(rowId, (Direction.DOWN, closestDown.Item2));
                }
            }
        }

        private (int, int) closestFilledRow(NativeHashMap<int, (Direction, int)> tmpResult, int rowId, Direction direction, int tmpDistance)
        {
            var newRow = direction switch
            {
                Direction.UP => rowId - 1,
                Direction.DOWN => rowId + 1,
                _ => throw new Exception("unknown direction")
            };
            if (newRow < 0 || newRow >= 10)
            {
                return (-1, -1);
            }

            if (tmpResult.TryGetValue(newRow, out var currentDirection) && currentDirection.Item1 == Direction.NONE)
            {
                return (tmpDistance, newRow);
            }

            return closestFilledRow(tmpResult, newRow, direction, tmpDistance + 1);
        }
    }
}