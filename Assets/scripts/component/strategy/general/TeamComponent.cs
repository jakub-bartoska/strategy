using Unity.Entities;

namespace component.strategy.general
{
    public struct TeamComponent : IComponentData
    {
        public Team team;
        public Entity teamMarker;
    }
}