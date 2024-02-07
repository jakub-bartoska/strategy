using System;
using component;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.enums;
using system.battle.utils;
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
            //row -> (team1, team2)
            var rowToTeamCount = new NativeHashMap<int, (int, int)>(10, Allocator.TempJob);

            fillBlockers(battalionPositions, fightPairs, movementBlockingPairs, possibleSplitDirections, rowToTeamCount);

            new FillPossibleSplits
                {
                    possibleSplitDirections = possibleSplitDirections
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var team1FlankPositions = new NativeHashMap<int, float3>(10, Allocator.TempJob);
            var team2FlankPositions = new NativeHashMap<int, float3>(10, Allocator.TempJob);

            var battalionMovementDirections = new NativeHashMap<long, Direction>(1000, Allocator.TempJob);
            foreach (var battalionPosition in battalionPositions.GetValueArray(Allocator.TempJob))
            {
                var direction = battalionPosition.Item3 switch
                {
                    Team.TEAM1 => Direction.LEFT,
                    Team.TEAM2 => Direction.RIGHT,
                    _ => throw new Exception("Unknown team")
                };
                battalionMovementDirections.Add(battalionPosition.Item1, direction);
            }

            fillFlankPositions(team1FlankPositions, team2FlankPositions, battalionPositions);

            var rowChanges = prepareRowSwitchTags(rowToTeamCount);
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            new AddRowChangeTags
                {
                    rowChanges = rowChanges,
                    ecb = ecb.AsParallelWriter(),
                    team1FlankPositions = team1FlankPositions,
                    team2FlankPositions = team2FlankPositions,
                    prefabHolder = prefabHolder
                }.ScheduleParallel(state.Dependency)
                .Complete();


            new ChangeMovementDirectionJob
                {
                    rowChanges = rowChanges,
                    team1FlankPositions = team1FlankPositions,
                    team2FlankPositions = team2FlankPositions,
                    battalionMovementDirections = battalionMovementDirections
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        private void fillBlockers(
            NativeParallelMultiHashMap<int, (long, float3, Team)> battalionPositions,
            DynamicBuffer<FightPair> fightPairs,
            DynamicBuffer<MovementBlockingPair> movementBlockingPairs,
            NativeParallelMultiHashMap<long, Direction> possibleSplitDirections,
            NativeHashMap<int, (int, int)> rowToTeamCount)
        {
            var allRows = battalionPositions.GetKeyArray(Allocator.TempJob);
            allRows.Sort();
            var uniqueCount = allRows.Unique();
            var uniqueRows = allRows.GetSubArray(0, uniqueCount);

            var sorter = new MovementSystem.SortByPosition();
            var rowBattalions = new NativeList<(long, float3, Team)>(Allocator.TempJob);
            var rowMinusOneUnsorted = new NativeList<(long, float3, Team)>(Allocator.TempJob);
            var rowPlusOneUnsorted = new NativeList<(long, float3, Team)>(Allocator.TempJob);
            //iterate over sorted rows
            foreach (var row in uniqueRows)
            {
                rowBattalions.Clear();
                rowMinusOneUnsorted.Clear();
                rowPlusOneUnsorted.Clear();

                foreach (var value in battalionPositions.GetValuesForKey(row))
                {
                    rowBattalions.Add(value);
                }

                foreach (var value in battalionPositions.GetValuesForKey(row - 1))
                {
                    rowMinusOneUnsorted.Add(value);
                }

                foreach (var value in battalionPositions.GetValuesForKey(row + 1))
                {
                    rowPlusOneUnsorted.Add(value);
                }

                rowBattalions.Sort(sorter);

                for (int i = 0; i < rowBattalions.Length; i++)
                {
                    addRowCounter(rowToTeamCount, row, rowBattalions[i].Item3);

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
                        if (isTooFar(closestPosition, myPosition, 11f))
                        {
                            possibleSplitDirections.Add(myId, Direction.RIGHT);
                            possibleSplitDirections.Add(closestId, Direction.LEFT);
                        }

                        continue;
                    }

                    if (myTeam == closestTeam)
                    {
                        movementBlockingPairs.Add(new MovementBlockingPair
                        {
                            blocker = myId,
                            victim = closestId,
                            direction = Direction.LEFT
                        });
                        movementBlockingPairs.Add(new MovementBlockingPair
                        {
                            blocker = closestId,
                            victim = myId,
                            direction = Direction.RIGHT
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

                    var upBlocked = false;
                    foreach (var (enemyId, enemyPosition, enemyTeam) in rowMinusOneUnsorted)
                    {
                        if (!isTooFar(myPosition, enemyPosition, 11f))
                        {
                            upBlocked = true;
                        }

                        //1.4f -> start fight little bit later than 2.5f
                        if (isTooFar(myPosition, enemyPosition, 0.1f)) continue;

                        if (myTeam != enemyTeam)
                        {
                            fightPairs.Add(new FightPair
                            {
                                battalionId1 = myId,
                                battalionId2 = enemyId,
                                fightType = BattalionFightType.VERTICAL
                            });
                        }
                    }

                    if (!upBlocked)
                    {
                        possibleSplitDirections.Add(myId, Direction.UP);
                    }

                    var downBlocked = false;
                    foreach (var (enemyId, enemyPosition, enemyTeam) in rowPlusOneUnsorted)
                    {
                        if (!isTooFar(myPosition, enemyPosition, 11f))
                        {
                            downBlocked = true;
                            break;
                        }
                    }

                    if (!downBlocked)
                    {
                        possibleSplitDirections.Add(myId, Direction.DOWN);
                    }
                }
            }
        }

        private NativeHashMap<int, (Team, Direction)> prepareRowSwitchTags(NativeHashMap<int, (int, int)> rowToTeamCount)
        {
            //row -> team, direction
            var result = new NativeHashMap<int, (Team, Direction)>(10, Allocator.TempJob);

            foreach (var row in rowToTeamCount)
            {
                if (row.Value is {Item1: 0, Item2: 0}) continue;
                if (row.Value.Item1 == 0)
                {
                    var upDirection = enemyBattalionsInDirection(rowToTeamCount, Team.TEAM2, Direction.UP, row.Key);
                    var downDirection = enemyBattalionsInDirection(rowToTeamCount, Team.TEAM2, Direction.DOWN, row.Key);
                    var resultDirection = upDirection > downDirection ? Direction.UP : Direction.DOWN;
                    result.Add(row.Key, (Team.TEAM2, resultDirection));
                }

                if (row.Value.Item2 == 0)
                {
                    var upDirection = enemyBattalionsInDirection(rowToTeamCount, Team.TEAM1, Direction.UP, row.Key);
                    var downDirection = enemyBattalionsInDirection(rowToTeamCount, Team.TEAM1, Direction.DOWN, row.Key);
                    var resultDirection = upDirection > downDirection ? Direction.UP : Direction.DOWN;
                    result.Add(row.Key, (Team.TEAM2, resultDirection));
                }
            }

            return result;
        }

        private int enemyBattalionsInDirection(NativeHashMap<int, (int, int)> rowToTeamCount, Team myTeam, Direction direction, int myRow)
        {
            var result = 0;

            var start = direction switch
            {
                Direction.UP => 0,
                Direction.DOWN => myRow + 1,
                _ => throw new Exception("Unknown direction")
            };

            var max = direction switch
            {
                Direction.UP => myRow,
                Direction.DOWN => 11,
                _ => throw new Exception("Unknown direction")
            };

            for (var i = start; i < max; i++)
            {
                if (rowToTeamCount.TryGetValue(i, out var teamCounts))
                {
                    switch (myTeam)
                    {
                        case Team.TEAM1:
                            result += teamCounts.Item2;
                            break;
                        case Team.TEAM2:
                            result += teamCounts.Item1;
                            break;
                        default:
                            throw new Exception("Unknown team");
                    }
                }
            }

            return result;
        }

        private void addRowCounter(NativeHashMap<int, (int, int)> rowToTeamCount, int row, Team team)
        {
            switch (team)
            {
                case Team.TEAM1:
                    if (rowToTeamCount.TryGetValue(row, out var team1Count))
                    {
                        rowToTeamCount[row] = (team1Count.Item1 + 1, team1Count.Item2);
                    }
                    else
                    {
                        rowToTeamCount.Add(row, (1, 0));
                    }

                    break;
                case Team.TEAM2:
                    if (rowToTeamCount.TryGetValue(row, out var team2Count))
                    {
                        rowToTeamCount[row] = (team2Count.Item1, team2Count.Item2 + 1);
                    }
                    else
                    {
                        rowToTeamCount.Add(row, (0, 1));
                    }

                    break;
                default:
                    throw new Exception("Unknown team");
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

        private void fillFlankPositions(NativeHashMap<int, float3> team1FlankPositions, NativeHashMap<int, float3> team2FlankPositions,
            NativeParallelMultiHashMap<int, (long, float3, Team)> battalionPositions)
        {
            for (var i = 0; i < 10; i++)
            {
                float3? team1Max = null;
                float3? team2Max = null;
                foreach (var valueTuple in battalionPositions.GetValuesForKey(i))
                {
                    if (valueTuple.Item3 == Team.TEAM1)
                    {
                        if (!team1Max.HasValue || team1Max.Value.x < valueTuple.Item2.x)
                        {
                            team1Max = valueTuple.Item2;
                        }
                    }
                    else if (valueTuple.Item3 == Team.TEAM2)
                    {
                        if (!team2Max.HasValue || team2Max.Value.x > valueTuple.Item2.x)
                        {
                            team2Max = valueTuple.Item2;
                        }
                    }
                }

                if (team1Max.HasValue) team2FlankPositions.Add(i, team1Max.Value);
                if (team2Max.HasValue) team1FlankPositions.Add(i, team2Max.Value);
            }
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

        [BurstCompile]
        [WithNone(typeof(ChangeRow))]
        public partial struct AddRowChangeTags : IJobEntity
        {
            [ReadOnly] public NativeHashMap<int, (Team, Direction)> rowChanges;
            [ReadOnly] public NativeHashMap<int, float3> team1FlankPositions;
            [ReadOnly] public NativeHashMap<int, float3> team2FlankPositions;
            [ReadOnly] public PrefabHolder prefabHolder;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(BattalionMarker battalionMarker, Entity entity, PossibleSplit split, LocalTransform transform)
            {
                if (rowChanges.TryGetValue(battalionMarker.row, out var teamDirection))
                {
                    var flankPosition = getFlankPosition(battalionMarker.row, teamDirection.Item2, battalionMarker.team);
                    var flankPossible = battalionMarker.team switch
                    {
                        Team.TEAM1 => flankPosition.x - 5 * 0.3f * 2f * 1.1f > transform.Position.x,
                        Team.TEAM2 => flankPosition.x + 5 * 0.3f * 2f * 1.1f < transform.Position.x,
                        _ => throw new Exception("Unknown team")
                    };
                    if (!flankPossible) return;

                    var canChange = isDirectionPossible(teamDirection.Item2, split);
                    if (!canChange) return;

                    var shadowEntity = BattalionShadowSpawner.spawnBattalionShadow(ecb, prefabHolder, transform.Position, battalionMarker.id);
                    ecb.AddComponent(0, entity, new ChangeRow
                    {
                        direction = teamDirection.Item2,
                        shadowEntity = shadowEntity
                    });
                }
            }

            private float3 getFlankPosition(int myRow, Direction direction, Team team)
            {
                var rowDelta = direction switch
                {
                    Direction.UP => -1,
                    Direction.DOWN => 1,
                    _ => throw new Exception("Unknown direction")
                };
                var flankPositions = team switch
                {
                    Team.TEAM1 => team1FlankPositions,
                    Team.TEAM2 => team2FlankPositions,
                    _ => throw new Exception("Unknown team")
                };
                if (flankPositions.TryGetValue(myRow + rowDelta, out var flankPosition))
                {
                    return flankPosition;
                }

                return getFlankPosition(myRow + rowDelta, direction, team);
            }

            private bool isDirectionPossible(Direction changeDirection, PossibleSplit possibleSplit)
            {
                return changeDirection switch
                {
                    Direction.UP => possibleSplit.up,
                    Direction.DOWN => possibleSplit.down,
                    Direction.LEFT => possibleSplit.left,
                    Direction.RIGHT => possibleSplit.right,
                    _ => throw new Exception("Unknown direction")
                };
            }
        }

        [BurstCompile]
        public partial struct ChangeMovementDirectionJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<int, (Team, Direction)> rowChanges;
            [ReadOnly] public NativeHashMap<int, float3> team1FlankPositions;
            [ReadOnly] public NativeHashMap<int, float3> team2FlankPositions;
            [ReadOnly] public NativeHashMap<long, Direction> battalionMovementDirections;

            private void Execute(BattalionMarker battalionMarker, LocalTransform transform, ref MovementDirection movementDirection)
            {
                var flank2 = getFlankPosition2(battalionMarker.row, battalionMarker.team);
                if (flank2.HasValue)
                {
                    var flankPossible = battalionMarker.team switch
                    {
                        Team.TEAM1 => flank2.Value.x > transform.Position.x,
                        Team.TEAM2 => flank2.Value.x < transform.Position.x,
                        _ => throw new Exception("Unknown team")
                    };
                    var resultDirection = battalionMarker.team switch
                    {
                        Team.TEAM1 => Direction.LEFT,
                        Team.TEAM2 => Direction.RIGHT,
                        _ => throw new Exception("Unknown team")
                    };
                    if (flankPossible)
                    {
                        resultDirection = battalionMarker.team switch
                        {
                            Team.TEAM1 => Direction.RIGHT,
                            Team.TEAM2 => Direction.LEFT,
                            _ => throw new Exception("Unknown team")
                        };
                    }

                    movementDirection.direction = resultDirection;
                    return;
                }

                if (rowChanges.TryGetValue(battalionMarker.row, out var teamDirection))
                {
                    var flankPosition = getFlankPosition(battalionMarker.row, teamDirection.Item2, battalionMarker.team);
                    var flankPossible = battalionMarker.team switch
                    {
                        Team.TEAM1 => flankPosition.x - 5 * 0.3f * 2f * 5f > transform.Position.x,
                        Team.TEAM2 => flankPosition.x + 5 * 0.3f * 2f * 5f < transform.Position.x,
                        _ => throw new Exception("Unknown team")
                    };
                    if (!flankPossible)
                    {
                        movementDirection.direction = battalionMovementDirections[battalionMarker.id];
                        return;
                    }

                    movementDirection.direction = teamDirection.Item2;
                }
            }

            private float3 getFlankPosition(int myRow, Direction direction, Team team)
            {
                var rowDelta = direction switch
                {
                    Direction.UP => -1,
                    Direction.DOWN => 1,
                    _ => throw new Exception("Unknown direction")
                };
                var flankPositions = team switch
                {
                    Team.TEAM1 => team1FlankPositions,
                    Team.TEAM2 => team2FlankPositions,
                    _ => throw new Exception("Unknown team")
                };
                if (flankPositions.TryGetValue(myRow + rowDelta, out var flankPosition))
                {
                    return flankPosition;
                }

                return getFlankPosition(myRow + rowDelta, direction, team);
            }

            private float3? getFlankPosition2(int myRow, Team team)
            {
                var flankPositions = team switch
                {
                    Team.TEAM1 => team1FlankPositions,
                    Team.TEAM2 => team2FlankPositions,
                    _ => throw new Exception("Unknown team")
                };
                if (flankPositions.TryGetValue(myRow, out var flankPosition))
                {
                    return flankPosition;
                }

                return null;
            }
        }
    }
}