using Unity.Entities;

namespace component.soldier.behavior.behaviors.shoot_arrow
{
    public struct ShootArrowContext : IComponentData
    {
        public float shootDelay;
        public float shootTimeRemaining;
    }
}