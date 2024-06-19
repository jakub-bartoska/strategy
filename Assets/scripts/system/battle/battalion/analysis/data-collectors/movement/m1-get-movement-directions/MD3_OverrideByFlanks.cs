using System;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(MD2_SetBasicDirections))]
    public partial struct MD3_OverrideByFlanks : ISystem
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
            foreach (var flankingBattalion in flankingBattalions)
            {
                var oldDirection = MovementDataHolder.plannedMovementDirections[flankingBattalion];
                var newDirection = getOppositeDirection(oldDirection);
                MovementDataHolder.plannedMovementDirections[flankingBattalion] = newDirection;
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