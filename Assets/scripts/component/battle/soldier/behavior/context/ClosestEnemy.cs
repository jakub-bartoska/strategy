using Unity.Entities;
using Unity.Mathematics;

namespace component.soldier
{
    public struct ClosestEnemy : IComponentData
    {
        public float3 closestEnemyPosition;
        public int2 closestEnemyCell;
        public float distanceFromClosestEnemy;
        public int closestEnemyId;
        public ClosestEnemyStatus status;
    }

    public enum ClosestEnemyStatus
    {
        HAS_ENEMY_WITH_POSITION,
        HAS_ENEMY_WITH_CELL,
        NO_ENEMY
    }
}