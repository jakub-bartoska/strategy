using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.horizontal_split
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(HS1_FindHorizontalSplitBlockers))]
    public partial struct HS2_FindSplitCandidates : ISystem
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
            var result = dataHolder.ValueRW.splitBattalions;
            //containns battalions which fight vertically only
            var verticalFighters = getVerticalFighters(dataHolder.ValueRO);
            removeBlockedBattalions(verticalFighters, movementDataHolder.ValueRO, dataHolder.ValueRO);

            var battalionDefaultMovementDirection = movementDataHolder.ValueRO.plannedMovementDirections;
            foreach (var verticalFighter in verticalFighters)
            {
                battalionDefaultMovementDirection.TryGetValue(verticalFighter.Key, out var direction);
                if (direction != Direction.NONE)
                {
                    result.Add(verticalFighter.Key, new SplitInfo
                    {
                        movamentDirrection = direction,
                        verticalFightType = verticalFighter.Value
                    });
                }
            }
        }

        private NativeHashMap<long, VerticalFightType> getVerticalFighters(DataHolder dataHolder)
        {
            var fightingPairs = dataHolder.fightingPairs;
            //UP/DOWN
            var verticalFights = new NativeHashMap<long, VerticalFightType>(1000, Allocator.Temp);
            //LEFT/RIGHT
            var normalFights = new NativeHashSet<long>(1000, Allocator.Temp);

            foreach (var fightingPair in fightingPairs)
            {
                switch (fightingPair.fightType)
                {
                    case BattalionFightType.NORMAL:
                        normalFights.Add(fightingPair.battalionId1);
                        normalFights.Add(fightingPair.battalionId2);
                        break;
                    case BattalionFightType.VERTICAL:
                        addVerticalFights(verticalFights, fightingPair.battalionId1, fightingPair.fightDirection);
                        addVerticalFights(verticalFights, fightingPair.battalionId2, getOppositeDeirection(fightingPair.fightDirection));
                        break;
                    default:
                        throw new Exception("Unknown fight type");
                }
            }

            foreach (var normalFightBattalionId in normalFights)
            {
                verticalFights.Remove(normalFightBattalionId);
            }

            return verticalFights;
        }

        private Direction getOppositeDeirection(Direction direction)
        {
            return direction switch
            {
                Direction.UP => Direction.DOWN,
                Direction.DOWN => Direction.UP,
                _ => throw new Exception("Unknown direction")
            };
        }

        private void addVerticalFights(NativeHashMap<long, VerticalFightType> verticalFights, long battalionId, Direction fightType)
        {
            var verticalFightType = directionToVerticalFightType(fightType);
            if (verticalFights.TryGetValue(battalionId, out var existingFightType))
            {
                var resultFightType = existingFightType switch
                {
                    VerticalFightType.UP => verticalFightType == VerticalFightType.DOWN ? VerticalFightType.BOTH : VerticalFightType.UP,
                    VerticalFightType.DOWN => verticalFightType == VerticalFightType.UP ? VerticalFightType.BOTH : VerticalFightType.DOWN,
                    VerticalFightType.BOTH => VerticalFightType.BOTH,
                    _ => throw new Exception("Unknown fight type")
                };
                verticalFights[battalionId] = resultFightType;
                return;
            }

            verticalFights.Add(battalionId, verticalFightType);
        }

        private VerticalFightType directionToVerticalFightType(Direction direction)
        {
            return direction switch
            {
                Direction.UP => VerticalFightType.UP,
                Direction.DOWN => VerticalFightType.DOWN,
                _ => throw new Exception("Unknown direction")
            };
        }

        private void removeBlockedBattalions(NativeHashMap<long, VerticalFightType> fightingBattalions, MovementDataHolder movementDataHolder, DataHolder dataHolder)
        {
            var plannedMovementDirections = movementDataHolder.plannedMovementDirections;
            var blockedHorizontalSplits = dataHolder.blockedHorizontalSplits;

            var blockedBattalions = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var fightingBattalion in fightingBattalions)
            {
                plannedMovementDirections.TryGetValue(fightingBattalion.Key, out var defaultDirection);
                foreach (var direction in blockedHorizontalSplits.GetValuesForKey(fightingBattalion.Key))
                {
                    if (direction == defaultDirection)
                    {
                        blockedBattalions.Add(fightingBattalion.Key);
                    }
                }
            }

            foreach (var blockedBattalion in blockedBattalions)
            {
                fightingBattalions.Remove(blockedBattalion);
            }
        }
    }
}