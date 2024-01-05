using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionMarker : IComponentData
    {
        public long id;
        public Team team;
        public int row;
    }
}