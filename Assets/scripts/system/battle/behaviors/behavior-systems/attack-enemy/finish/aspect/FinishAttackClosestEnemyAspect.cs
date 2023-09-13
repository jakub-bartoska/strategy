using component.soldier;
using component.soldier.behavior.behaviors;
using component.soldier.behavior.fight;
using ProjectDawn.Navigation;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    public readonly partial struct FinishAttackClosestEnemyAspect : IAspect
    {
        private readonly RefRW<BehaviorContext> context;

        public void execute()
        {
            var contextRW = context.ValueRW;
            if (contextRW.behaviorToBeFinished != BehaviorType.FIGHT)
            {
                return;
            }

            contextRW.behaviorToBeFinished = BehaviorType.NONE;
        }
    }
}