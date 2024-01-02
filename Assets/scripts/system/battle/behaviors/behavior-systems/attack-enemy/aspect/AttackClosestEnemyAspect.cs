using component.config.authoring_pairs;
using component.helpers;
using component.soldier;
using component.soldier.behavior.behaviors;
using component.soldier.behavior.fight;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    public readonly partial struct AttackClosestEnemyAspect : IAspect
    {
        private readonly RefRO<ClosestEnemy> closestEnemy;
        private readonly RefRW<FightContext> fightContext;
        private readonly RefRO<BehaviorContext> context;

        public void attackClosestEnemy(float deltaTime, DynamicBuffer<Damage> damage, MeeleConfig meeleConfig)
        {
            if (context.ValueRO.currentBehavior != BehaviorType.FIGHT)
            {
                return;
            }

            var remainingDelay = fightContext.ValueRO.attackTimeRemaining;
            remainingDelay -= deltaTime;
            if (remainingDelay < 0)
            {
                remainingDelay = fightContext.ValueRO.attackDelay;
                var receiverId = closestEnemy.ValueRO.closestEnemyId;
                damage.Add(new Damage
                {
                    dmgReceiverId = receiverId,
                    dmgAmount = (int) meeleConfig.meeleDamage
                });
            }

            fightContext.ValueRW.attackTimeRemaining = remainingDelay;
        }
    }
}