using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    public partial struct AnalyseBattlefieldSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var battalionPositions = new NativeParallelMultiHashMap<int, (long, float3, Team)>(1000, Allocator.TempJob);
            new CollectBattalionPositionsJob
                {
                    battalionPositions = battalionPositions.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var fightPairs = SystemAPI.GetSingletonBuffer<FightPair>();
            var movementBlockingPairs = SystemAPI.GetSingletonBuffer<MovementBlockingPair>();

            fightPairs.Clear();
            movementBlockingPairs.Clear();

            var possibleSplitDirections = new NativeParallelMultiHashMap<long, Direction>(4000, Allocator.TempJob);

            fillBlockers(battalionPositions, fightPairs, movementBlockingPairs, possibleSplitDirections);

            new FillPossibleSplits
                {
                    possibleSplitDirections = possibleSplitDirections
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        private void fillBlockers(
            NativeParallelMultiHashMap<int, (long, float3, Team)> battalionPositions,
            DynamicBuffer<FightPair> fightPairs,
            DynamicBuffer<MovementBlockingPair> movementBlockingPairs,
            NativeParallelMultiHashMap<long, Direction> possibleSplitDirections)
        {
            var allRows = battalionPositions.GetKeyArray(Allocator.TempJob);
            allRows.Sort();
            var uniqueCount = allRows.Unique();
            var uniqueRows = allRows.GetSubArray(0, uniqueCount);

            var sorter = new MovementSystem.SortByPosition();
            //iterate over sorted rows
            var firstRow = true;
            foreach (var row in uniqueRows)
            {
                //sort battalions in row + row above
                var rowBattalions = new NativeList<(long, float3, Team)>(Allocator.TempJob);
                var rowMinusOne = new NativeList<(long, float3, Team)>(Allocator.TempJob);
                foreach (var value in battalionPositions.GetValuesForKey(row))
                {
                    rowBattalions.Add(value);
                }

                foreach (var value in battalionPositions.GetValuesForKey(row - 1))
                {
                    rowMinusOne.Add(value);
                }

                rowMinusOne.Sort(sorter);
                rowBattalions.Sort(sorter);

                for (int i = 0; i < rowBattalions.Length; i++)
                {
                    var (myId, myPosition, myTeam) = rowBattalions[i];

                    if (i == 0)
                    {
                        possibleSplitDirections.Add(myId, Direction.LEFT);
                    }

                    //last battalion has no interaction
                    if (i == rowBattalions.Length - 1)
                    {
                        possibleSplitDirections.Add(myId, Direction.RIGHT);
                        continue;
                    }

                    var (closestId, closestPosition, closestTeam) = rowBattalions[i + 1];

                    if (isTooFar(closestPosition, myPosition, 5f))
                    {
                        if (isTooFar(closestPosition, myPosition, 8f))
                        {
                            possibleSplitDirections.Add(myId, Direction.RIGHT);
                            possibleSplitDirections.Add(closestId, Direction.LEFT);
                        }

                        continue;
                    }

                    if (myTeam == closestTeam)
                    {
                        var blocker = myTeam == Team.TEAM2 ? closestId : myId;
                        var victim = myTeam == Team.TEAM2 ? myId : closestId;
                        movementBlockingPairs.Add(new MovementBlockingPair
                        {
                            blocker = blocker,
                            victim = victim
                        });
                        continue;
                    }

                    fightPairs.Add(new FightPair
                    {
                        battalionId1 = myId,
                        battalionId2 = closestId,
                        fightType = BattalionFightType.NORMAL
                    });
                }

                for (int i = 0; i < rowBattalions.Length; i++)
                {
                    var (myId, myPosition, myTeam) = rowBattalions[i];

                    if (firstRow)
                    {
                        possibleSplitDirections.Add(myId, Direction.UP);
                    }
                    else if (row == uniqueRows[^1])
                    {
                        possibleSplitDirections.Add(myId, Direction.DOWN);
                    }

                    foreach (var (enemyId, enemyPosition, enemyTeam) in rowMinusOne)
                    {
                        if (myTeam == enemyTeam) continue;

                        if (isTooFar(myPosition, enemyPosition, 5f))
                        {
                            possibleSplitDirections.Add(myId, Direction.UP);
                            possibleSplitDirections.Add(enemyId, Direction.DOWN);
                        }

                        //1.4f -> start fight little bit later than 2.5f
                        if (isTooFar(myPosition, enemyPosition, 0.1f)) continue;

                        fightPairs.Add(new FightPair
                        {
                            battalionId1 = myId,
                            battalionId2 = enemyId,
                            fightType = BattalionFightType.VERTICAL
                        });
                    }
                }

                firstRow = false;
            }
        }

        private bool isTooFar(float3 position1, float3 position2, float targetDistance)
        {
            var distance = math.abs(position1.x - position2.x);
            // 5 = 1/2 size of battalion
            // 0.3 = size modifier used on model
            // 2 = 2 battalions
            // 1.1 = safety margin
            return distance > (targetDistance * 0.3 * 2 * 1.1f);
        }

        [BurstCompile]
        public partial struct CollectBattalionPositionsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, (long, float3, Team)>.ParallelWriter battalionPositions;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform)
            {
                battalionPositions.Add(battalionMarker.row,
                    (battalionMarker.id, transform.Position, battalionMarker.team));
            }
        }

        [BurstCompile]
        public partial struct FillPossibleSplits : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<long, Direction> possibleSplitDirections;

            private void Execute(BattalionMarker battalionMarker, ref PossibleSplit split)
            {
                split.up = false;
                split.down = false;
                split.left = false;
                split.right = false;
                foreach (var direction in possibleSplitDirections.GetValuesForKey(battalionMarker.id))
                {
                    switch (direction)
                    {
                        case Direction.UP:
                            split.up = true;
                            break;
                        case Direction.DOWN:
                            split.down = true;
                            break;
                        case Direction.LEFT:
                            split.left = true;
                            break;
                        case Direction.RIGHT:
                            split.right = true;
                            break;
                    }
                }
            }
        }
    }
}