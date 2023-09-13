using system;
using system.general;
using Unity.Entities;

namespace system_groups
{
    [UpdateBefore(typeof(DamageSystem))]
    [UpdateAfter(typeof(FinishBehaviorSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BehaviorSystemGroup : ComponentSystemGroup
    {
    }
}