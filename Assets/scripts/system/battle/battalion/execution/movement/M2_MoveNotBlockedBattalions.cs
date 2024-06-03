﻿using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.execution.movement;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(M1_SetFlanks))]
    public partial struct M2_MoveNotBlockedBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = DataHolder.blockers;

            //battalionID -> direction to move
            var battalionsAbleToMove = new NativeList<(long, Direction)>(1000, Allocator.TempJob);
            var battalionsPerformingAction = DataHolder.battalionsPerformingAction;
            var allBattalionIds = DataHolder.allBattalionIds;
            var exactPositionMovementDirections = DataHolder.exactPositionMovementDirections;

            foreach (var battalionId in allBattalionIds)
            {
                if (battalionsPerformingAction.Contains(battalionId))
                {
                    if (!exactPositionMovementDirections.ContainsKey(battalionId))
                    {
                        continue;
                    }
                }

                var blockedForDirection = false;
                var direction = getDirection(battalionId);
                foreach (var valueTuple in blockers.GetValuesForKey(battalionId))
                {
                    if (valueTuple.Item3 == direction)
                    {
                        blockedForDirection = true;
                        break;
                    }
                }

                if (!blockedForDirection)
                {
                    battalionsAbleToMove.Add((battalionId, direction));
                }
            }

            var ableToMoveInDefaultDirection = BlockerUtils.unblockDirections(battalionsAbleToMove);
            foreach (var valueTuple in battalionsAbleToMove)
            {
                ableToMoveInDefaultDirection.Add(valueTuple.Item1, valueTuple.Item2);
            }

            new UpdateMovementDirectionJob
                {
                    ableToMoveInDefaultDirection = ableToMoveInDefaultDirection
                }.Schedule(state.Dependency)
                .Complete();
        }

        private Direction getDirection(long battalionId)
        {
            if (DataHolder.exactPositionMovementDirections.TryGetValue(battalionId, out var direction))
            {
                return direction.Item1;
            }

            return DataHolder.battalionDefaultMovementDirection[battalionId];
        }

        [BurstCompile]
        public partial struct UpdateMovementDirectionJob : IJobEntity
        {
            public NativeHashMap<long, Direction> ableToMoveInDefaultDirection;

            private void Execute(BattalionMarker battalionMarker, ref MovementDirection movementDirection)
            {
                if (ableToMoveInDefaultDirection.TryGetValue(battalionMarker.id, out var direction))
                {
                    movementDirection.currentDirection = direction;
                }
                else
                {
                    movementDirection.currentDirection = Direction.NONE;
                }
            }
        }
    }
}