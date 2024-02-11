using component.config.game_settings;
using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionMarker : IComponentData
    {
        public long id;
        public SoldierType soldierType;
    }
}