using Unity.Entities;

namespace component
{
    public struct SoldierStatus : IComponentData
    {
        public int index;
        public Team team;
        public long companyId;
    }

    public enum Team
    {
        TEAM1,
        TEAM2
    }
}