using System;
using component;
using component._common.system_switchers;
using component.helpers.positioning;
using component.pathfinding;
using component.soldier;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.positions.position_holder
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ParsePositionsToPositionHolderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PositionHolderConfig>();
            state.RequireForUpdate<PositionHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var positionHolderConfig = SystemAPI.GetSingleton<PositionHolderConfig>();
            var positionHolder = SystemAPI.GetSingleton<PositionHolder>();
            positionHolder.soldierIdPosition.Clear();
            positionHolder.team1PositionCells.Clear();
            positionHolder.team2PositionCells.Clear();
            state.Dependency.Complete();
            new PositionParserJob
                {
                    team1Positions = positionHolder.team1PositionCells.AsParallelWriter(),
                    team2Positions = positionHolder.team2PositionCells.AsParallelWriter(),
                    idPositions = positionHolder.soldierIdPosition.AsParallelWriter(),
                    positionHolderConfig = positionHolderConfig
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var team1Cells = getUniqueKeys(positionHolder.team1PositionCells);
            var team2Cells = getUniqueKeys(positionHolder.team2PositionCells);

            if (team1Cells.Length == 0 || team2Cells.Length == 0)
            {
                team1Cells.Dispose();
                team2Cells.Dispose();
                new FillNoEnemyJob()
                    .ScheduleParallel(state.Dependency)
                    .Complete();
                return;
            }

            var team1CellsWithTooCloseEnemy = new NativeList<int2>(team1Cells.Length, Allocator.TempJob);
            var team2CellsWithTooCloseEnemy = new NativeList<int2>(team2Cells.Length, Allocator.TempJob);
            var team1ClosestCells = new NativeParallelMultiHashMap<int2, int2>(team1Cells.Length, Allocator.TempJob);
            var team2ClosestCells = new NativeParallelMultiHashMap<int2, int2>(team2Cells.Length, Allocator.TempJob);

            var closestHandle = new FindClosestJob
            {
                primaryTeam = team1Cells,
                enemyTeam = team2Cells,
                cellsWithTooCloseEnemy = team1CellsWithTooCloseEnemy.AsParallelWriter(),
                closestPairs = team1ClosestCells.AsParallelWriter(),
            }.Schedule(team1Cells.Length, 20);

            var closestHandle2 = new FindClosestJob
            {
                primaryTeam = team2Cells,
                enemyTeam = team1Cells,
                cellsWithTooCloseEnemy = team2CellsWithTooCloseEnemy.AsParallelWriter(),
                closestPairs = team2ClosestCells.AsParallelWriter(),
            }.Schedule(team2Cells.Length, 20);

            JobHandle.CombineDependencies(closestHandle, closestHandle2)
                .Complete();

            new FillClosestEnemy
                {
                    team1Postions = positionHolder.team1PositionCells,
                    team2Postions = positionHolder.team2PositionCells,
                    idPositions = positionHolder.soldierIdPosition,
                    team1ClosestPairs = team1ClosestCells,
                    team2ClosestPairs = team2ClosestCells,
                    positionHolderConfig = positionHolderConfig
                }.ScheduleParallel(state.Dependency)
                .Complete();

            team1CellsWithTooCloseEnemy.Dispose();
            team2CellsWithTooCloseEnemy.Dispose();
            team1ClosestCells.Dispose();
            team2ClosestCells.Dispose();
            team1Cells.Dispose();
            team2Cells.Dispose();
        }

        private NativeArray<int2> getUniqueKeys(NativeParallelMultiHashMap<int2, int> map)
        {
            var keys = map.GetKeyArray(Allocator.TempJob);
            var uniqueCount = keys.Unique();
            return keys.GetSubArray(0, uniqueCount);
        }
    }

    [BurstCompile]
    public partial struct PositionParserJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, int>.ParallelWriter team1Positions;
        public NativeParallelMultiHashMap<int2, int>.ParallelWriter team2Positions;
        public NativeParallelMultiHashMap<int, float3>.ParallelWriter idPositions;
        [NativeDisableUnsafePtrRestriction] public PositionHolderConfig positionHolderConfig;

        private void Execute(SoldierStatus soldierStatus, RefRO<LocalTransform> localTransform)
        {
            var position = localTransform.ValueRO.Position.xz;
            var positionKey = getMapKey(position);

            var soldierId = soldierStatus.index;

            if (soldierStatus.team == Team.TEAM1)
            {
                team1Positions.Add(positionKey, soldierId);
            }
            else
            {
                team2Positions.Add(positionKey, soldierId);
            }

            idPositions.Add(soldierId, localTransform.ValueRO.Position);
        }

        private int2 getMapKey(float2 position)
        {
            var xOffset = positionHolderConfig.minSquarePosition.x;
            var localXPlusOffset = position.x - xOffset;
            var xColumn = (int) (localXPlusOffset / positionHolderConfig.oneSquareSize);

            var yOffset = positionHolderConfig.minSquarePosition.y;
            var localYPlusOffset = position.y - yOffset;
            var yColumn = (int) (localYPlusOffset / positionHolderConfig.oneSquareSize);

            return new int2(xColumn, yColumn);
        }
    }

    [BurstCompile]
    public partial struct FindClosestJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int2> primaryTeam;
        [ReadOnly] public NativeArray<int2> enemyTeam;
        public NativeList<int2>.ParallelWriter cellsWithTooCloseEnemy;
        public NativeParallelMultiHashMap<int2, int2>.ParallelWriter closestPairs;

        public void Execute(int index)
        {
            var cell = primaryTeam[index];
            var closestCell = enemyTeam[0];
            var closestCellDistance = float.MaxValue;

            var minSqrtDistance = math.distancesq(new int2(0, 0), new int2(2, 2));

            foreach (var enemy in enemyTeam)
            {
                var currentSqrtDistance = math.distancesq(cell, enemy);
                if (currentSqrtDistance <= minSqrtDistance)
                {
                    cellsWithTooCloseEnemy.AddNoResize(cell);
                    return;
                }

                if (currentSqrtDistance < closestCellDistance)
                {
                    closestCell = enemy;
                    closestCellDistance = currentSqrtDistance;
                }
            }

            closestPairs.Add(cell, closestCell);
        }
    }

    [BurstCompile]
    public partial struct FillNoEnemyJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(ref ClosestEnemy closestEnemy)
        {
            closestEnemy.status = ClosestEnemyStatus.NO_ENEMY;
        }
    }

    [BurstCompile]
    public partial struct FillClosestEnemy : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, int> team1Postions;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int> team2Postions;
        [ReadOnly] public NativeParallelMultiHashMap<int, float3> idPositions;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int2> team1ClosestPairs;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int2> team2ClosestPairs;
        [NativeDisableUnsafePtrRestriction] public PositionHolderConfig positionHolderConfig;

        private void Execute(SoldierStatus soldierStatus, LocalTransform localTransform, ref ClosestEnemy closestEnemy)
        {
            var myPosition = localTransform.Position.xz;
            var positionKey = getMapKey(myPosition);

            var myTeam = soldierStatus.team;
            var closestPairsMap = myTeam == Team.TEAM1 ? team1ClosestPairs : team2ClosestPairs;

            if (closestPairsMap.TryGetFirstValue(positionKey, out var item, out _))
            {
                var targetPosition = keyToPosition(item);

                closestEnemy.closestEnemyPosition = targetPosition;
                closestEnemy.closestEnemyCell = item;
                closestEnemy.closestEnemyId = -1;
                closestEnemy.distanceFromClosestEnemy = math.distance(myPosition, targetPosition.xz);
                closestEnemy.status = ClosestEnemyStatus.HAS_ENEMY_WITH_CELL;

                return;
            }

            var enemyCellPositions = myTeam == Team.TEAM1 ? team2Postions : team1Postions;
            var closestEnemyId = getClosestCells(positionKey, enemyCellPositions, myPosition);

            if (idPositions.TryGetFirstValue(closestEnemyId, out var enemyPosition, out _))
            {
                closestEnemy.closestEnemyPosition = enemyPosition;
                closestEnemy.closestEnemyId = closestEnemyId;
                closestEnemy.distanceFromClosestEnemy = math.distance(myPosition, enemyPosition.xz);
                closestEnemy.status = ClosestEnemyStatus.HAS_ENEMY_WITH_POSITION;
            }
        }

        private int getClosestCells(
            int2 myCell,
            NativeParallelMultiHashMap<int2, int> enemyCellPositions,
            float2 position
        )
        {
            var result = (-1, float.MaxValue);

            var cell = myCell;
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //vrsek
            cell = new int2(myCell.x - 1, myCell.y - 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 1, myCell.y);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 1, myCell.y + 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //leva
            cell = new int2(myCell.x, myCell.y - 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //strop
            cell = new int2(myCell.x + 1, myCell.y - 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 1, myCell.y);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 1, myCell.y + 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //prava
            cell = new int2(myCell.x, myCell.y + 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);

            if (result.Item1 != -1)
            {
                return result.Item1;
            }

            //spodni
            cell = new int2(myCell.x - 2, myCell.y - 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 2, myCell.y - 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 2, myCell.y);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 2, myCell.y + 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x - 2, myCell.y + 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //leva
            cell = new int2(myCell.x - 1, myCell.y - 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x, myCell.y - 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 1, myCell.y - 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //horni
            cell = new int2(myCell.x + 2, myCell.y - 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 2, myCell.y - 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 2, myCell.y);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 2, myCell.y + 1);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 2, myCell.y + 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            //prava
            cell = new int2(myCell.x - 1, myCell.y + 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x, myCell.y + 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);
            cell = new int2(myCell.x + 1, myCell.y + 2);
            result = cellsToEnemyIds(cell, enemyCellPositions, position, result);

            if (result.Item1 == -1)
            {
                throw new Exception("Chyba, tohle by nemelo nikdy nastat");
            }

            return result.Item1;
        }

        private (int, float) cellsToEnemyIds(
            int2 cell,
            NativeParallelMultiHashMap<int2, int> enemyCellPositions,
            float2 myPosition,
            (int, float) previousMinEnemyIdDistance
        )
        {
            var result = previousMinEnemyIdDistance;
            foreach (var enemyId in enemyCellPositions.GetValuesForKey(cell))
            {
                if (idPositions.TryGetFirstValue(enemyId, out var enemyPosition, out _))
                {
                    var enemySqrtDistance = math.distancesq(myPosition, enemyPosition.xz);
                    if (enemySqrtDistance < result.Item2)
                    {
                        result = (enemyId, enemySqrtDistance);
                    }
                }
            }

            return result;
        }

        private int2 getMapKey(float2 position)
        {
            var xOffset = positionHolderConfig.minSquarePosition.x;
            var localXPlusOffset = position.x - xOffset;
            var xColumn = (int) (localXPlusOffset / positionHolderConfig.oneSquareSize);

            var yOffset = positionHolderConfig.minSquarePosition.y;
            var localYPlusOffset = position.y - yOffset;
            var yColumn = (int) (localYPlusOffset / positionHolderConfig.oneSquareSize);

            return new int2(xColumn, yColumn);
        }

        private int3 keyToPosition(int2 key)
        {
            var xPositionWithoutOffset =
                (key.x * positionHolderConfig.oneSquareSize) + (positionHolderConfig.oneSquareSize * 0.5);
            var xOffset = positionHolderConfig.minSquarePosition.x;
            var x = xPositionWithoutOffset + xOffset;

            var yPositionWithoutOffset =
                (key.y * positionHolderConfig.oneSquareSize) + (positionHolderConfig.oneSquareSize * 0.5);
            var yOffset = positionHolderConfig.minSquarePosition.y;
            var y = yPositionWithoutOffset + yOffset;

            return new int3((int) x, 0, (int) y);
        }
    }
}