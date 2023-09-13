using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    public readonly partial struct FinishFollowClosestEnemyAspect : IAspect
    {
        private readonly RefRW<BehaviorContext> context;
        private readonly RefRW<AgentBody> agentBody;

        public void execute()
        {
            if (context.ValueRO.behaviorToBeFinished != BehaviorType.FOLLOW_CLOSEST_ENEMY)
            {
                return;
            }

            context.ValueRW.behaviorToBeFinished = BehaviorType.NONE;
            agentBody.ValueRW.Stop();
        }
    }
}