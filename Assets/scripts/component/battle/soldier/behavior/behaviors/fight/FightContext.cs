using Unity.Entities;

namespace component.soldier.behavior.fight
{
    public struct FightContext : IComponentData
    {
        public float attackDelay;
        public float attackTimeRemaining;
    }
}