using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct MovementDirection : IComponentData
    {
        //direction my team should move
        public Direction defaultDirection;

        //can be overriden when battalion is flanking
        public Direction plannedDirection;

        //can be overriden by current manever
        public Direction currentDirection;
    }
}