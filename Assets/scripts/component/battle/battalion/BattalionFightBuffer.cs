using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionFightBuffer : IBufferElementData
    {
        public float time;
        public long enemyBattalionId;
    }
}