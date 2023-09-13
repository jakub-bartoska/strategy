using Unity.Entities;

namespace component.config.authoring_pairs
{
    public struct ArrowConfig : IComponentData
    {
        public float arrowShootingDelay;
        public float arrowFlightSpeed;
        public int arrowDamage;
        public float shootingDistance;
        public float overshootRatio;
    }
}