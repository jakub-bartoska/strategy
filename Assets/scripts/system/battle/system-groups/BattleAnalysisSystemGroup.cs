using Unity.Entities;

namespace system.battle.system_groups
{
    [UpdateAfter(typeof(BattleCleanupSystemGroup))]
    public partial class BattleAnalysisSystemGroup : ComponentSystemGroup
    {
    }
}