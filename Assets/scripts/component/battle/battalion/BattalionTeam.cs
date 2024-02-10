using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionTeam : IComponentData
    {
        public Team value;
    }
}