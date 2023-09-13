using component._common.system_switchers;
using system_groups;
using system.behaviors.behavior_systems.shoot_arrow.finish.aspect;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.behavior_systems.shoot_arrow.finish
{
    [BurstCompile]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct ShootArrowFinishSystem : ISystem
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
            new FinishShootArrowJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct FinishShootArrowJob : IJobEntity
    {
        private void Execute(ShootArrowFinishAspect aspect)
        {
            aspect.execute();
        }
    }
}