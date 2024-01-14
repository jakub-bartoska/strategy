using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct PossibleSplit : IComponentData
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
    }
}