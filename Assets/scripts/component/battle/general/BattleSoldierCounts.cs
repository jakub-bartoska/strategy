using Unity.Entities;

namespace component.general
{
    public struct BattleSoldierCounts : IComponentData
    {
        public int team1Count;
        public int team2Count;
    }
}