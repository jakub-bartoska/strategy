using System;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.row_change;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(RC3_MoveBetweenRows))]
    public partial struct M1_SetFlanks : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var flankingBattalions = DataHolder.flankingBattalions;

            new SetFlanksJob
                {
                    flankingBattalions = flankingBattalions
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct SetFlanksJob : IJobEntity
        {
            public NativeHashSet<long> flankingBattalions;

            private void Execute(BattalionMarker battalionMarker, ref MovementDirection movementDirection)
            {
                if (flankingBattalions.Contains(battalionMarker.id))
                {
                    movementDirection.plannedDirection = getOppositeDirection(movementDirection.defaultDirection);
                }
                else
                {
                    movementDirection.plannedDirection = movementDirection.defaultDirection;
                }
            }

            private Direction getOppositeDirection(Direction direction)
            {
                return direction switch
                {
                    Direction.LEFT => Direction.RIGHT,
                    Direction.RIGHT => Direction.LEFT,
                    Direction.UP => Direction.DOWN,
                    Direction.DOWN => Direction.UP,
                    _ => throw new Exception("Unknown direction")
                };
            }
        }
    }
}