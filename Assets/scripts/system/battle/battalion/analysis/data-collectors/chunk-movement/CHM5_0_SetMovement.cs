using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_5_BasicChunkMovement))]
    public partial struct CHM5_0_SetMovement : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var plannedMovementDirections = movementDataHolder.ValueRW.plannedMovementDirections;

            var backupPlanDataHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();

            foreach (var battalionId in backupPlanDataHolder.ValueRW.moveLeft)
            {
                plannedMovementDirections.Add(battalionId, Direction.LEFT);
            }

            foreach (var battalionId in backupPlanDataHolder.ValueRW.moveRight)
            {
                plannedMovementDirections.Add(battalionId, Direction.RIGHT);
            }
        }
    }
}