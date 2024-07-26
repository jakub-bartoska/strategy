namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(F2_FindFlankBattalions))]
    [UpdateAfter(typeof(CHM4_SetMovement))]
    public partial struct MD1_DefaultDirectionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var battalionDefaultMovementDirection = movementDataHolder.ValueRW.battalionDefaultMovementDirection;
            new CollectBattalionDirections
                {
                    battalionDirections = battalionDefaultMovementDirection
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectBattalionDirections : IJobEntity
        {
            public NativeHashMap<long, Direction> battalionDirections;

            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection)
            {
                battalionDirections.Add(battalionMarker.id, movementDirection.defaultDirection);
            }
        }
    }
}