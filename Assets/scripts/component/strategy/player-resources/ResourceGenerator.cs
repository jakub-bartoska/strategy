using _Monobehaviors.ui.player_resources;
using Unity.Entities;

namespace component.strategy.player_resources
{
    public struct ResourceGenerator : IBufferElementData
    {
        public ResourceType type;
        public int value;
        public float timeRemaining;
        public float defaultTimer;
    }
}