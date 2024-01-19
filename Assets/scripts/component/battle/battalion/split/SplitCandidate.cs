using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct SplitCandidate : IBufferElementData
    {
        public long battalionId;
        public Direction direction;
        public SplitType type;
    }

    public enum SplitType
    {
        //all soldiers without 2 are moved to the new battalion
        MINUS_TWO,

        //everyone is moved to new battalion
        ALL
    }
}