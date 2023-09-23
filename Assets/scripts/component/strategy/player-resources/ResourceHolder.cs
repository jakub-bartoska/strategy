using _Monobehaviors.ui.player_resources;
using Unity.Entities;

namespace component.strategy.player_resources
{
    public struct ResourceHolder : IBufferElementData
    {
        public ResourceType type;
        public long value;
    }
}