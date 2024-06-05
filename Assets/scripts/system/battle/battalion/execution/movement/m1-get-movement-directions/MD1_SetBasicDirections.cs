using component.battle.battalion;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.battalion.row_change;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(RC3_MoveBetweenRows))]
    public partial struct MD1_SetBasicDirections : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var defaultDirections = MovementDataHolder.battalionDefaultMovementDirection;
            foreach (var defaultDirection in defaultDirections)
            {
                MovementDataHolder.plannedMovementDirections.Add(defaultDirection.Key, defaultDirection.Value);
            }
        }
    }
}