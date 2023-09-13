using component.authoring_pairs;
using component.config.authoring_pairs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.projectiles.arrows.aspect
{
    public readonly partial struct ArrowFlightAspect : IAspect
    {
        public readonly RefRW<ArrowMarker> arrowMarker;
        public readonly RefRW<LocalTransform> transform;
        public readonly Entity entity;

        public void execute(float deltaTime, EntityCommandBuffer ecb, ArrowConfig arrowConfig)
        {
            transform.ValueRW.Position += arrowMarker.ValueRO.direction * deltaTime * arrowConfig.arrowFlightSpeed;
            arrowMarker.ValueRW.lifeRemaining -= deltaTime;
            if (arrowMarker.ValueRO.lifeRemaining <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}