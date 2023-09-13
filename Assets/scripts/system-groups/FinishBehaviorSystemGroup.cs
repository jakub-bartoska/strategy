using system;
using Unity.Entities;

namespace system_groups
{
    [UpdateAfter(typeof(BehaviorPickerSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class FinishBehaviorSystemGroup : ComponentSystemGroup
    {
        
    }
}