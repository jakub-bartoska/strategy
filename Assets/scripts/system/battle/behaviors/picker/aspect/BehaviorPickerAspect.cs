using component.config.authoring_pairs;
using component.formation;
using component.soldier;
using component.soldier.behavior.behaviors;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace system
{
    [BurstCompile]
    public readonly partial struct BehaviorPickerAspect : IAspect
    {
        private readonly RefRW<BehaviorContext> context;
        private readonly RefRO<ClosestEnemy> closestEnemy;
        private readonly RefRO<SoldierFormationStatus> soldierFormationStatus;

        [BurstCompile]
        public void pickBestBehavior(
            ArrowConfig arrowConfig
        )
        {
            var currentBehavior = context.ValueRO.currentBehavior;
            var newBehavior = pickNewBehavior(arrowConfig);

            if (currentBehavior == newBehavior)
            {
                return;
            }

            context.ValueRW.currentBehavior = newBehavior;
            context.ValueRW.behaviorToBeFinished = currentBehavior;
        }

        private BehaviorType pickNewBehavior(
            ArrowConfig arrowConfig
        )
        {
            var availableBehaviors = context.ValueRO.possibleBehaviors;

            if (closestEnemy.ValueRO.status == ClosestEnemyStatus.NO_ENEMY)
            {
                /*
                if (soldierFormationStatus.ValueRO.formationStatus == FormationStatus.NO_FORMATION
                    && containsBehavior(availableBehaviors, BehaviorType.MAKE_LINE_FORMATION))
                {
                    return BehaviorType.MAKE_LINE_FORMATION;
                }

                if (soldierFormationStatus.ValueRO.formationStatus == FormationStatus.IN_FORMATION
                    && containsBehavior(availableBehaviors, BehaviorType.PROCESS_FORMATION_COMMAND))
                {
                    return BehaviorType.PROCESS_FORMATION_COMMAND;
                }
                */

                return BehaviorType.IDLE;
            }

            if (containsBehavior(availableBehaviors, BehaviorType.SHOOT_ARROW) &&
                closestEnemy.ValueRO.distanceFromClosestEnemy < arrowConfig.shootingDistance)
            {
                return BehaviorType.SHOOT_ARROW;
            }

            if (containsBehavior(availableBehaviors, BehaviorType.FIGHT) &&
                closestEnemy.ValueRO.distanceFromClosestEnemy < 2)
            {
                return BehaviorType.FIGHT;
            }

            if (containsBehavior(availableBehaviors, BehaviorType.MOVE_FORWARD))
            {
                return BehaviorType.MOVE_FORWARD;
            }

            if (soldierFormationStatus.ValueRO.formationStatus == FormationStatus.NO_FORMATION
                && containsBehavior(availableBehaviors, BehaviorType.MAKE_LINE_FORMATION))
            {
                return BehaviorType.MAKE_LINE_FORMATION;
            }

            if (soldierFormationStatus.ValueRO.formationStatus == FormationStatus.IN_FORMATION
                && containsBehavior(availableBehaviors, BehaviorType.PROCESS_FORMATION_COMMAND))
            {
                return BehaviorType.PROCESS_FORMATION_COMMAND;
            }

            return BehaviorType.IDLE;
        }

        private bool containsBehavior(UnsafeList<BehaviorType> list, BehaviorType type)
        {
            foreach (var behaviorType in list)
            {
                if (behaviorType == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}