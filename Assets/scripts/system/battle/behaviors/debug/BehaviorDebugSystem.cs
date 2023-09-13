using component._common.system_switchers;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.debug
{
    [BurstCompile]
    [UpdateAfter(typeof(BehaviorPickerSystem))]
    public partial struct BehaviorDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DrawDebugSoldiersJob().ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct DrawDebugSoldiersJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(TestAspect aspect)
        {
            aspect.execute();
        }
    }
}