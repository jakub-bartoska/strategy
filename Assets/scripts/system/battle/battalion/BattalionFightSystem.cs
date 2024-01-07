using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(BattalionMovementSystem))]
    public partial struct BattalionFightSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            new PerformBattalionFightJob
                {
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct PerformBattalionFightJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionFightBuffer> battalionFight)
            {
                if (battalionFight.Length == 0) return;
            }
        }
    }
}