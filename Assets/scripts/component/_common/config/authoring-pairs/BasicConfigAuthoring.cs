using Unity.Entities;
using UnityEngine;

namespace component.config.authoring_pairs
{
    public class BasicConfigAuthoring : MonoBehaviour
    {
        public float arrowShootingDelay = 0.5f;
        public float arrowFlightSpeed = 60;

        public int arrowDamage = 20;

        //vzdalenost na ktery se ma spustit shoot behavior
        public float shootingDistance = 40;

        //o kolik ma preltet sip svuj cil (1 = 100%)
        public float overshootRatio = 1.3f;

        public float meeleDamage = 10f;
    }

    public class BasicConfigBaker : Baker<BasicConfigAuthoring>
    {
        public override void Bake(BasicConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArrowConfig
            {
                arrowDamage = authoring.arrowDamage,
                arrowFlightSpeed = authoring.arrowFlightSpeed,
                arrowShootingDelay = authoring.arrowShootingDelay,
                shootingDistance = authoring.shootingDistance,
                overshootRatio = authoring.overshootRatio
            });
            AddComponent(entity, new MeeleConfig
            {
                meeleDamage = authoring.meeleDamage
            });
        }
    }
}