using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.backup_plans;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM5_2_SetMovementForRestBattalions))]
    public partial struct FindFinalDamageDealersSystem : ISystem
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
            var fightingPairs = dataHolder.ValueRO.fightingPairs;
            var plannedMovementDirections = SystemAPI.GetSingletonRW<MovementDataHolder>().ValueRO.plannedMovementDirections;

            var tmpResult = new NativeParallelMultiHashMap<long, BattalionFightTarget>(1000, Allocator.Temp);
            var finalResult = new NativeParallelMultiHashMap<long, BattalionFightTarget>(1000, Allocator.Temp);

            foreach (var fightingPair in fightingPairs)
            {
                tmpResult.Add(fightingPair.battalionId1, new BattalionFightTarget
                {
                    targetBattalionId = fightingPair.battalionId2,
                    direction = fightingPair.fightDirection,
                    fightWeight = getWeight(fightingPair.fightDirection, fightingPair.battalionId1, plannedMovementDirections),
                    fightType = fightingPair.fightType
                });
                tmpResult.Add(fightingPair.battalionId2, new BattalionFightTarget
                {
                    targetBattalionId = fightingPair.battalionId1,
                    direction = getOpositeDirection(fightingPair.fightDirection),
                    fightWeight = getWeight(getOpositeDirection(fightingPair.fightDirection), fightingPair.battalionId2, plannedMovementDirections),
                    fightType = fightingPair.fightType
                });
            }

            prepareResult(tmpResult, finalResult);

            dataHolder.ValueRW.battalionDamages = finalResult;
        }

        private Direction getOpositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.UP => Direction.DOWN,
                Direction.DOWN => Direction.UP,
                Direction.LEFT => Direction.RIGHT,
                Direction.RIGHT => Direction.LEFT,
                _ => throw new Exception("Invalid direction " + direction)
            };
        }

        private int getWeight(Direction direction, long battalionId, NativeHashMap<long, Direction> plannedMovementDirections)
        {
            return direction switch
            {
                Direction.UP => 1,
                Direction.DOWN => 1,
                Direction.LEFT => getHorizontalWeight(battalionId, direction, plannedMovementDirections),
                Direction.RIGHT => getHorizontalWeight(battalionId, direction, plannedMovementDirections),
                _ => throw new Exception("Invalid direction " + direction)
            };
        }

        private int getHorizontalWeight(long battalionId, Direction direction, NativeHashMap<long, Direction> plannedMovementDirections)
        {
            if (plannedMovementDirections.TryGetValue(battalionId, out var plannedDirection))
            {
                if (plannedDirection == direction)
                {
                    return 2;
                }
            }

            return 1;
        }

        private void prepareResult(NativeParallelMultiHashMap<long, BattalionFightTarget> tmpResult, NativeParallelMultiHashMap<long, BattalionFightTarget> finalResult)
        {
            foreach (var battalionId in tmpResult.GetKeyArray(Allocator.Temp))
            {
                //add all verticals
                foreach (var battalionFightTarget in tmpResult.GetValuesForKey(battalionId))
                {
                    if (battalionFightTarget.direction == Direction.UP || battalionFightTarget.direction == Direction.DOWN)
                    {
                        finalResult.Add(battalionId, battalionFightTarget);
                    }
                }

                //add best horizontal
                var bestHorizontal = getBestHorizontal(tmpResult.GetValuesForKey(battalionId));
                if (bestHorizontal.HasValue)
                {
                    finalResult.Add(battalionId, bestHorizontal.Value);
                }
            }
        }

        private BattalionFightTarget? getBestHorizontal(NativeParallelMultiHashMap<long, BattalionFightTarget>.Enumerator enumerator)
        {
            BattalionFightTarget? result = null;

            foreach (var battalionFightTarget in enumerator)
            {
                if (battalionFightTarget.direction == Direction.UP || battalionFightTarget.direction == Direction.DOWN)
                {
                    continue;
                }

                if (!result.HasValue || result.Value.fightWeight < battalionFightTarget.fightWeight)
                {
                    result = battalionFightTarget;
                }
            }

            return result;
        }
    }
}