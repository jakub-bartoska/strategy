using component.battle.battalion;
using system.battle.battalion.analysis;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(MD1_DefaultDirectionsSystem))]
    public partial struct MD2_SetBasicDirections : ISystem
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