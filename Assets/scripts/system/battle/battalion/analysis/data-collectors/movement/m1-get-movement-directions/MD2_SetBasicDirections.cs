using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis;
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
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var defaultDirections = movementDataHolder.ValueRO.battalionDefaultMovementDirection;
            foreach (var defaultDirection in defaultDirections)
            {
                movementDataHolder.ValueRW.plannedMovementDirections.Add(defaultDirection.Key, defaultDirection.Value);
            }
        }
    }
}