using Unity.Collections;
using Unity.Entities;

namespace component.strategy.events
{
    public struct CreateNewArmyEvent : IBufferElementData
    {
        public NativeList<long> companiesToDeploy;
    }
}