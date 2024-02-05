using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct MovementBlockingPair : IBufferElementData
    {
        public long blocker;
        public long victim;
        public Direction direction;
    }
}