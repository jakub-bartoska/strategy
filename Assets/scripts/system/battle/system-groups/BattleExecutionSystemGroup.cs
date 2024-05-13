using Unity.Entities;

namespace system.battle.system_groups
{
    [UpdateAfter(typeof(BattleDecisionSystemGroup))]
    public partial class BattleExecutionSystemGroup : ComponentSystemGroup
    {
    }
}