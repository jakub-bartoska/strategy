using component.battle.battalion;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(MD2_OverrideByFlanks))]
    public partial struct MD3_AdjustByBattleMovements : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inFightMovement = MovementDataHolder.inFightMovement;
            foreach (var battalionDirectionDistance in inFightMovement)
            {
                var newDirection = battalionDirectionDistance.Value.Item1;
                MovementDataHolder.plannedMovementDirections[battalionDirectionDistance.Key] = newDirection;
            }
        }
    }
}