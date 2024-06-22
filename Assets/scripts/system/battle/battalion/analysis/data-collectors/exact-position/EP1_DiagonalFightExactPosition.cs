using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.horizontal_split;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace system.battle.battalion.analysis.exact_position
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(HS3_RemoveWaitingBattalions))]
    public partial struct EP1_DiagonalFightExactPosition : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DataHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<MovementDataHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingleton<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            // can contain dupolicities (A -> B and B -> A)
            // battalion id -> all battalion IDS which are in diagonal fight with this battalion
            // if battalion has at least 1 normal fight, it is removed from this list
            var onlyDiagonalFights = getOnlyDiagonalFights(dataHolder);

            getMovementDirection(onlyDiagonalFights, dataHolder, movementDataHolder);
        }

        private NativeParallelMultiHashMap<long, long> getOnlyDiagonalFights(DataHolder dataHolder)
        {
            var result = new NativeParallelMultiHashMap<long, long>(1000, Allocator.Temp);
            var normalFights = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var fightingPair in dataHolder.fightingPairs)
            {
                switch (fightingPair.fightType)
                {
                    case BattalionFightType.NORMAL:
                        normalFights.Add(fightingPair.battalionId1);
                        normalFights.Add(fightingPair.battalionId2);
                        break;
                    case BattalionFightType.VERTICAL:
                        result.Add(fightingPair.battalionId1, fightingPair.battalionId2);
                        result.Add(fightingPair.battalionId2, fightingPair.battalionId1);
                        break;
                    default:
                        throw new Exception("Unknown fight type");
                }
            }

            foreach (var normalFight in normalFights)
            {
                result.Remove(normalFight);
            }

            return result;
        }

        private void getMovementDirection(NativeParallelMultiHashMap<long, long> diagonalFights, DataHolder dataHolder, RefRW<MovementDataHolder> movementDataHolder)
        {
            var battalionInfo = dataHolder.battalionInfo;
            var keys = diagonalFights.GetKeyArray(Allocator.Temp);
            foreach (var key in keys)
            {
                var myPosition = battalionInfo[key].position;
                var direction = Direction.NONE;
                var conflict = false;
                var minXDistance = -1f;
                var minDistanceEnemyId = -1L;
                foreach (var enemyId in diagonalFights.GetValuesForKey(key))
                {
                    var enemyPosition = battalionInfo[enemyId].position;
                    var xDistance = myPosition.x - enemyPosition.x;
                    if (xDistance < 0)
                    {
                        if (direction != Direction.NONE && direction != Direction.RIGHT)
                        {
                            conflict = true;
                            break;
                        }

                        direction = Direction.RIGHT;
                    }
                    else if (xDistance > 0)
                    {
                        if (direction != Direction.NONE && direction != Direction.LEFT)
                        {
                            conflict = true;
                            break;
                        }

                        direction = Direction.LEFT;
                    }
                    //distance is the same
                    else
                    {
                        //battalion should nto move since it is in exactPosition
                        direction = Direction.NONE;
                        conflict = true;
                    }

                    if (Mathf.Approximately(minXDistance, -1f) || Math.Abs(xDistance) < minXDistance)
                    {
                        minXDistance = Math.Abs(xDistance);
                        minDistanceEnemyId = enemyId;
                    }
                }

                if (!conflict && direction != Direction.NONE)
                {
                    movementDataHolder.ValueRW.inFightMovement.Add(key, (direction, minXDistance, minDistanceEnemyId));
                }
            }
        }
    }
}