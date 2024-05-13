using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattleUnitType : IComponentData
    {
        //duplicit information from markers
        public long id;
        public BattleUnitTypeEnum type;
    }

    public enum BattleUnitTypeEnum
    {
        BATTALION,
        SHADOW
    }
}