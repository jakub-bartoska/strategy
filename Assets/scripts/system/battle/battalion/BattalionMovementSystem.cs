using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    public partial struct BattalionMovementSystem : ISystem
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
                }.Schedule(state.Dependency)
                .Complete();

            //var forwardMovementBlocker = new NativeParallelHashMap<int, bool>(1000, Allocator.TempJob);
            var battalionWillingMove = new NativeParallelHashMap<long, bool>(1000, Allocator.TempJob);
            // battalion id fights battalion with id -> contains duplicities
            var battalionFights = new NativeHashMap<long, long>(1000, Allocator.TempJob);

            fillBlockers(battalionPositions, battalionWillingMove, battalionFights);

            var deltaTime = SystemAPI.Time.DeltaTime;
            new MoveBattalionJob
                {
                    deltaTime = deltaTime,
                    willingToMove = battalionWillingMove
                }.Schedule(state.Dependency)
                .Complete();

            new AddInBattleTagJob
                {
                    battalionFights = battalionFights
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        private void fillBlockers(NativeParallelMultiHashMap<int, (long, float3, Team)> battalionPositions, NativeParallelHashMap<long, bool> willingToMove, NativeHashMap<long, long> battalionFights)
        {
            var allRows = battalionPositions.GetKeyArray(Allocator.TempJob);
            allRows.Sort();
            var uniqueCount = allRows.Unique();
            var uniqueRows = allRows.GetSubArray(0, uniqueCount);
            foreach (var row in uniqueRows)
            {
                var rowBattalions = new NativeList<(long, float3, Team)>(Allocator.TempJob);
                foreach (var value in battalionPositions.GetValuesForKey(row))
                {
                    rowBattalions.Add(value);
                }

                rowBattalions.Sort(new SortByTeamAndPosition());

                for (int i = 0; i < rowBattalions.Length; i++)
                {
                    var myBattalion = rowBattalions[i];

                    if (willingToMove.ContainsKey(myBattalion.Item1))
                    {
                        goto outerLoop;
                    }

                    if (rowBattalions.Length == 1)
                    {
                        willingToMove.Add(myBattalion.Item1, true);
                        continue;
                    }

                    (long, float3, Team) closestEnemy;
                    if (myBattalion.Item3 == Team.TEAM2)
                    {
                        if (rowBattalions.Length - 1 > i)
                        {
                            closestEnemy = rowBattalions[i + 1];
                        }
                        else
                        {
                            willingToMove.Add(myBattalion.Item1, true);
                            continue;
                        }
                    }
                    else
                    {
                        if (i != 0)
                        {
                            closestEnemy = rowBattalions[i - 1];
                        }
                        else
                        {
                            willingToMove.Add(myBattalion.Item1, true);
                            continue;
                        }
                    }

                    if (closestEnemy.Item3 == myBattalion.Item3)
                    {
                        willingToMove.Add(myBattalion.Item1, true);
                        continue;
                    }

                    if (isTooFar(closestEnemy.Item2, myBattalion.Item2))
                    {
                        willingToMove.Add(myBattalion.Item1, true);
                        continue;
                    }

                    battalionFights.Add(myBattalion.Item1, closestEnemy.Item1);
                    willingToMove.Add(myBattalion.Item1, false);

                    if (myBattalion.Item3 == Team.TEAM2)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (!isTooFar(rowBattalions[j].Item2, rowBattalions[j + 1].Item2))
                            {
                                willingToMove[rowBattalions[j].Item1] = false;
                            }
                            else
                            {
                                willingToMove[rowBattalions[j].Item1] = true;
                            }
                        }
                    }
                    else
                    {
                        for (int j = i + 1; j < rowBattalions.Length; j++)
                        {
                            if (!isTooFar(rowBattalions[j].Item2, rowBattalions[j - 1].Item2))
                            {
                                willingToMove[rowBattalions[j].Item1] = false;
                            }
                            else
                            {
                                willingToMove[rowBattalions[j].Item1] = true;
                            }
                        }
                    }
                }

                outerLoop:
                continue;
            }
        }

        private bool isTooFar(float3 position1, float3 position2)
        {
            var distance = math.abs(position1.x - position2.x);
            // 5 = 1/2 size of battalion
            // 0.3 = size modifier used on model
            // 2 = 2 battalions
            // 1.1 = safety margin
            return distance > (5f * 0.3 * 2 * 1.1f);
        }

        public class SortByTeamAndPosition : IComparer<(long, float3, Team)>
        {
            public int Compare((long, float3, Team) e1, (long, float3, Team) e2)
            {
                if (e1.Item3 == e2.Item3)
                {
                    return e1.Item2.x.CompareTo(e2.Item2.x);
                }

                if (e1.Item3 == Team.TEAM1) return 1;

                return -1;
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
        public partial struct MoveBattalionJob : IJobEntity
        {
            public float deltaTime;
            public NativeParallelHashMap<long, bool> willingToMove;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform)
            {
                if (willingToMove[battalionMarker.id] == false) return;

                var speed = 10f * deltaTime;
                var direction = battalionMarker.team == Team.TEAM1 ? -1 : 1;

                var delta = new float3(direction * speed, 0, 0);
                transform.Position += delta;
            }
        }

        [BurstCompile]
        public partial struct AddInBattleTagJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<long, long> battalionFights;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionFightBuffer> battalionFight)
            {
                if (battalionFights.TryGetValue(battalionMarker.id, out var enemyId))
                {
                    var exists = false;
                    foreach (var fight in battalionFight)
                    {
                        if (fight.enemyBattalionId == enemyId)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        battalionFight.Add(new BattalionFightBuffer
                        {
                            time = 1f,
                            enemyBattalionId = enemyId
                        });
                    }
                }
            }
        }
    }
}