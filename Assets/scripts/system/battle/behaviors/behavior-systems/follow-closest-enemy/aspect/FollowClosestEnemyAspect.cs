using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    public readonly partial struct FollowClosestEnemyAspect : IAspect
    {
        private readonly RefRW<AgentBody> agentBody;
        private readonly RefRO<ClosestEnemy> closestEnemy;
        private readonly RefRO<BehaviorContext> context;

        public void followClosestEnemy()
        {
            if (context.ValueRO.currentBehavior != BehaviorType.FOLLOW_CLOSEST_ENEMY)
            {
                return;
            }

            agentBody.ValueRW.IsStopped = false;
            agentBody.ValueRW.Destination = closestEnemy.ValueRO.closestEnemyPosition;
        }
    }
}