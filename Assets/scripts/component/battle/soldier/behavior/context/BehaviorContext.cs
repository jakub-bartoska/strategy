using component.soldier.behavior.behaviors;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace component.soldier
{
    public struct BehaviorContext : IComponentData
    {
        public BehaviorType currentBehavior;
        public UnsafeList<BehaviorType> possibleBehaviors;
        public BehaviorType behaviorToBeFinished;
    }
}