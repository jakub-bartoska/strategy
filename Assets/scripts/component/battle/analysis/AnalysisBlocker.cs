using Unity.Entities;

namespace component.battle.analysis
{
    public struct AnalysisBlocker : IBufferElementData
    {
        public long battalionId;
        public long? blockerTop;
        public long? blockerBot;
        public long? blockerLeft;
        public long? blockerRight;
    }
}