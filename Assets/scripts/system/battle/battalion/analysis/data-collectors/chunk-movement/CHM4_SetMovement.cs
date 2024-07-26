namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_BasicChunkMovement))]
    public partial struct CHM4_SetMovement : ISystem
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

            foreach (var battalionInfo in backupPlanDataHolder.ValueRW.moveLeft)
            {
                plannedMovementDirections.Add(battalionInfo.battalionId, Direction.LEFT);
            }

            foreach (var battalionInfo in backupPlanDataHolder.ValueRW.moveRight)
            {
                plannedMovementDirections.Add(battalionInfo.battalionId, Direction.RIGHT);
            }

            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var allBattalionIds = dataHolder.ValueRO.allBattalionIds;

            foreach (var battalionId in allBattalionIds)
            {
                if (!plannedMovementDirections.ContainsKey(battalionId))
                {
                    plannedMovementDirections.Add(battalionId, Direction.NONE);
                }
            }

            var battalionSwitchRowDirections = dataHolder.ValueRO.battalionSwitchRowDirections;

            foreach (var battalionInfo in backupPlanDataHolder.ValueRO.moveToDifferentChunk)
            {
                battalionSwitchRowDirections.Add(battalionInfo.battalionId, Direction.UP);
            }
        }
    }
}