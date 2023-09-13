using System;
using component;
using component.authoring_pairs;
using component.config.authoring_pairs;
using component.general;
using component.helpers.positioning;
using component.soldier;
using component.soldier.behavior.behaviors;
using component.soldier.behavior.behaviors.shoot_arrow;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.behaviors.behavior_systems.shoot_arrow.aspect
{
    public readonly partial struct ShootArrowAspect : IAspect
    {
        private readonly RefRO<SoldierStatus> soldierStatus;
        private readonly RefRW<ShootArrowContext> shootContext;
        private readonly RefRO<LocalTransform> transform;
        private readonly RefRO<ClosestEnemy> closestEnemy;
        private readonly RefRO<BehaviorContext> context;

        public void execute(EntityCommandBuffer.ParallelWriter ecb, Entity entity, float deltaTime,
            ArrowConfig arrowConfig, PositionHolder positionHolder)
        {
            if (context.ValueRO.currentBehavior != BehaviorType.SHOOT_ARROW)
            {
                return;
            }

            shootContext.ValueRW.shootTimeRemaining -= deltaTime;

            if (shootContext.ValueRW.shootTimeRemaining <= 0)
            {
                shootContext.ValueRW.shootTimeRemaining = shootContext.ValueRO.shootDelay;
                shootArrow(ecb, entity, soldierStatus.ValueRO.team, arrowConfig, positionHolder);
            }
        }

        private void shootArrow(EntityCommandBuffer.ParallelWriter ecb, Entity entity, Team team,
            ArrowConfig arrowConfig, PositionHolder positionHolder)
        {
            var arrow = ecb.Instantiate(soldierStatus.ValueRO.index, entity);
            ecb.SetName(soldierStatus.ValueRO.index, arrow, "Arrow");
            var arrowPosition = new float3(
                transform.ValueRO.Position.x,
                1,
                transform.ValueRO.Position.z
            );

            var arrowDirection = getArrowDirection(positionHolder);

            var quaternion = Unity.Mathematics.quaternion.Euler(arrowDirection);
            var arrowTransform = LocalTransform.FromPositionRotation(arrowPosition, quaternion);
            var lifeRemaining = arrowConfig.shootingDistance / arrowConfig.arrowFlightSpeed *
                                arrowConfig.overshootRatio;
            var arrowMarker = new ArrowMarker
            {
                direction = arrowDirection,
                lifeRemaining = lifeRemaining,
                team = team
            };

            ecb.AddComponent(soldierStatus.ValueRO.index, arrow, new BattleCleanupTag());
            ecb.AddComponent(soldierStatus.ValueRO.index, arrow, arrowMarker);
            ecb.SetComponent(soldierStatus.ValueRO.index, arrow, arrowTransform);
        }

        private float3 getArrowDirection(PositionHolder positionHolder)
        {
            var enemyPosition = getClosestEnemyPosition(positionHolder);

            var direction = enemyPosition - transform.ValueRO.Position;
            direction.y = 0;
            return math.normalize(direction);
        }

        private float3 getClosestEnemyPosition(PositionHolder positionHolder)
        {
            switch (closestEnemy.ValueRO.status)
            {
                case ClosestEnemyStatus.HAS_ENEMY_WITH_POSITION:
                    return closestEnemy.ValueRO.closestEnemyPosition;
                case ClosestEnemyStatus.HAS_ENEMY_WITH_CELL:
                    return getClosestEnemyPositionFromCell(positionHolder, closestEnemy.ValueRO.closestEnemyCell);
                default:
                    throw new Exception("This should not happen");
            }
        }

        private float3 getClosestEnemyPositionFromCell(PositionHolder positionHolder, int2 cell)
        {
            var myPosition = transform.ValueRO.Position;
            var cellPositions = soldierStatus.ValueRO.team == Team.TEAM1
                ? positionHolder.team2PositionCells
                : positionHolder.team1PositionCells;
            var soldierIdPositions = positionHolder.soldierIdPosition;
            var closestDistanceSqrt = float.MaxValue;
            var closestEnemyPosition = new float3(500, 0, 500);
            foreach (var enemyId in cellPositions.GetValuesForKey(cell))
            {
                if (soldierIdPositions.TryGetFirstValue(enemyId, out var enemyPosition, out _))
                {
                    var enemySqrtDistance = math.distancesq(myPosition, enemyPosition);
                    if (enemySqrtDistance < closestDistanceSqrt)
                    {
                        closestDistanceSqrt = enemySqrtDistance;
                        closestEnemyPosition = enemyPosition;
                    }
                }
            }

            return closestEnemyPosition;
        }
    }
}