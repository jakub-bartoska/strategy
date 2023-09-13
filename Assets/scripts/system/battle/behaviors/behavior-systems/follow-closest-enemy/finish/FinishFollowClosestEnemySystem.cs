using component._common.movement_agents;
using component._common.system_switchers;
using system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FinishBehaviorSystemGroup))]
    public partial struct FinishFollowClosestEnemySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentMovementAllowedForBattleTag>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FinishFollowClosestJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct FinishFollowClosestJob : IJobEntity
    {
        private void Execute(FinishFollowClosestEnemyAspect aspect)
        {
            aspect.execute();
        }
    }
}