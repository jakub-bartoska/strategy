using System;
using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
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
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // can contain dupolicities (A -> B and B -> A)
            // battalion id -> all battalion IDS which are in diagonal fight with this battalion
            // if battalion has at least 1 normal fight, it is removed from this list
            var onlyDiagonalFights = getOnlyDiagonalFights();

            getMovementDirection(onlyDiagonalFights);
        }

        private NativeParallelMultiHashMap<long, long> getOnlyDiagonalFights()
        {
            var result = new NativeParallelMultiHashMap<long, long>(1000, Allocator.Temp);
            var normalFights = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var fightingPair in DataHolder.fightingPairs)
            {
                switch (fightingPair.Item3)
                {
                    case BattalionFightType.NORMAL:
                        normalFights.Add(fightingPair.Item1);
                        normalFights.Add(fightingPair.Item2);
                        break;
                    case BattalionFightType.VERTICAL:
                        result.Add(fightingPair.Item1, fightingPair.Item2);
                        result.Add(fightingPair.Item2, fightingPair.Item1);
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

        private void getMovementDirection(NativeParallelMultiHashMap<long, long> diagonalFights)
        {
            var battalionInfo = DataHolder.battalionInfo;
            var keys = diagonalFights.GetKeyArray(Allocator.Temp);
            foreach (var key in keys)
            {
                var myPosition = battalionInfo[key].Item1;
                var direction = Direction.NONE;
                var conflict = false;
                var minXDistance = -1f;
                foreach (var enemyId in diagonalFights.GetValuesForKey(key))
                {
                    var enemyPosition = battalionInfo[enemyId].Item1;
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
                    }
                }

                if (!conflict && direction != Direction.NONE)
                {
                    MovementDataHolder.inFightMovement.Add(key, (direction, minXDistance));
                }
            }
        }
    }
}