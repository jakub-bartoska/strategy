using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.exact_position;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.movement
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(MD3_OverrideByFlanks))]
    [UpdateAfter(typeof(EP2_ExactPositionForEnemies))]
    public partial struct MD4_AdjustByBattleMovements : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var inFightMovement = movementDataHolder.ValueRO.inFightMovement;
            foreach (var battalionDirectionDistance in inFightMovement)
            {
                var newDirection = battalionDirectionDistance.Value.direction;
                movementDataHolder.ValueRW.plannedMovementDirections[battalionDirectionDistance.Key] = newDirection;
            }
        }
    }
}