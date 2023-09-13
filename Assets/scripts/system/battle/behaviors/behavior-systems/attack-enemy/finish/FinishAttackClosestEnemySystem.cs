using component._common.system_switchers;
using system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FinishBehaviorSystemGroup))]
    public partial struct FinishAttackClosestEnemySystem : ISystem
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
            new FinishAttackClosestEnemyJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct FinishAttackClosestEnemyJob : IJobEntity
    {
        private void Execute(FinishAttackClosestEnemyAspect aspect)
        {
            aspect.execute();
        }
    }
}