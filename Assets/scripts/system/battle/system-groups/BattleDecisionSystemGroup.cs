using Unity.Entities;

namespace system.battle.system_groups
{
    [UpdateAfter(typeof(BattleAnalysisSystemGroup))]
    public partial class BattleDecisionSystemGroup : ComponentSystemGroup
    {
    }
}