using Unity.Entities;

namespace system.battle.system_groups
{
    [UpdateAfter(typeof(BattleExecutionSystemGroup))]
    public partial class BattleSetResultsSystemGroup : ComponentSystemGroup
    {
    }
}