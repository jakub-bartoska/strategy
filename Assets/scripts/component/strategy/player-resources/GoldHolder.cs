using Unity.Entities;

namespace component.strategy.player_resources
{
    public struct GoldHolder : IComponentData
    {
        public long gold;
        public int goldPerSecond;
        public float timeRemaining;
    }
}