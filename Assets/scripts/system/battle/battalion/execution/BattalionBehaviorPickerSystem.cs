using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public partial struct BattalionBehaviorPickerSystem : ISystem
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
            var battalionDefaultMovementDirection = DataHolder.battalionDefaultMovementDirection;

            //battalionID -> direction to move
            var battalionsAbleToMove = new NativeList<(long, Direction)>(1000, Allocator.TempJob);
            var notMovingBattalions = DataHolder.notMovingBattalions;
            var allBattalionIds = DataHolder.allBattalionIds;

            foreach (var battalionId in allBattalionIds)
            {
                if (notMovingBattalions.Contains(battalionId))
                {
                    continue;
                }

                var blockedForDirection = false;
                var direction = battalionDefaultMovementDirection[battalionId];
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
                ableToMoveInDefaultDirection.Add(valueTuple.Item1);
            }

            new UpdateMovementDirectionJob
                {
                    ableToMoveInDefaultDirection = ableToMoveInDefaultDirection
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct UpdateMovementDirectionJob : IJobEntity
        {
            public NativeList<long> ableToMoveInDefaultDirection;

            private void Execute(BattalionMarker battalionMarker, ref MovementDirection movementDirection)
            {
                if (ableToMoveInDefaultDirection.Contains(battalionMarker.id))
                {
                    movementDirection.currentDirection = movementDirection.defaultDirection;
                }
                else
                {
                    movementDirection.currentDirection = Direction.NONE;
                }
            }
        }
    }
}