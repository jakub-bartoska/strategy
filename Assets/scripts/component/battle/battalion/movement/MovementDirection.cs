using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct MovementDirection : IComponentData
    {
        public Direction direction;
    }
}