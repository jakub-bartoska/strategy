using Unity.Entities;

namespace component._common.general
{
    public struct GamePlayerSettings : IComponentData
    {
        public Team playerTeam;
    }
}