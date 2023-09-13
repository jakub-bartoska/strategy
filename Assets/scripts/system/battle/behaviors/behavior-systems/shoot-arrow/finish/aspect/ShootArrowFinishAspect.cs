using component.soldier;
using component.soldier.behavior.behaviors;
using component.soldier.behavior.behaviors.shoot_arrow;
using component.soldier.behavior.fight;
using Unity.Entities;

namespace system.behaviors.behavior_systems.shoot_arrow.finish.aspect
{
    public readonly partial struct ShootArrowFinishAspect : IAspect
    {
        private readonly RefRW<BehaviorContext> context;

        public void execute()
        {
            var contextRW = context.ValueRW;
            if (contextRW.behaviorToBeFinished != BehaviorType.SHOOT_ARROW)
            {
                return;
            }

            contextRW.behaviorToBeFinished = BehaviorType.NONE;
        }
    }
}