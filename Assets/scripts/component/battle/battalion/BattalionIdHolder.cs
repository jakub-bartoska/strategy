using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionIdHolder : IComponentData
    {
        public long nextBattalionId;
    }
}