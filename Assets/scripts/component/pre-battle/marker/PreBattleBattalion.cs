using component.config.game_settings;
using Unity.Entities;
using Unity.Mathematics;

namespace component.pre_battle.marker
{
    public struct PreBattleBattalion : IBufferElementData
    {
        public float3 position;
        public Entity entity;

        //stable
        public SoldierType? soldierType;
        public Team? team;
        public long? battalionId;

        //temporary
        public SoldierType? soldierTypeTmp;
        public Team? teamTmp;
        public long? battalionIdTmp;

        public bool marked;
    }
}