using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct FightPair : IBufferElementData
    {
        public long battalionId1;
        public long battalionId2;
        public BattalionFightType fightType;
    }
}