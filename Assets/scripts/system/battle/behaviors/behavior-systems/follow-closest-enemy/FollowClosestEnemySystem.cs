using component._common.movement_agents;
using component._common.system_switchers;
using system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct FollowClosestEnemySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentMovementAllowedForBattleTag>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //todo nemuze to delat performance issue?
            new FollowClosestJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct FollowClosestJob : IJobEntity
    {
        private void Execute(FollowClosestEnemyAspect followClosestEnemyAspect)
        {
            followClosestEnemyAspect.followClosestEnemy();
        }
    }
}