using Unity.Entities;

namespace component.battle.battalion
{
    public struct PossibleReinforcements : IBufferElementData
    {
        public long needHelpBattalionId;
        public long canHelpBattalionId;
    }
}