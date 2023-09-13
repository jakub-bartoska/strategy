using Unity.Entities;
using Unity.Mathematics;

namespace component.strategy.army_components
{
    public struct ArmyMovementStatus : IComponentData
    {
        public MovementType movementType;
        public float3? targetPosition;
        public long? targetArmyId;
        public Team? targetArmyTeam;
        public long? targetTownId;
    }

    public enum MovementType
    {
        ENTER_TOWN,
        FOLLOW_ARMY,
        MOVE,
        IDLE
    }
}