using _Monobehaviors.resource;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.minor_objects;
using component.strategy.player_resources;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Serialization;

namespace system.strategy.ui
{
    public partial struct MinorResourcesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<InterfaceState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var interfaceState = SystemAPI.GetSingleton<InterfaceState>();
            if (interfaceState.state != UIState.MINOR_UI) return;

            var markedMinorResources = new NativeList<ResourceHolder>(10, Allocator.TempJob);
            new CollectMarkedMinorResources
                {
                    markedMinorResources = markedMinorResources
                }.Schedule(state.Dependency)
                .Complete();

            MinorResource.instance.updateResources(markedMinorResources);
        }

        public partial struct CollectMarkedMinorResources : IJobEntity
        {
            [FormerlySerializedAs("markedTownResources")]
            public NativeList<ResourceHolder> markedMinorResources;

            private void Execute(MinorTag minorTag, Marked marked, DynamicBuffer<ResourceHolder> resources)
            {
                markedMinorResources.AddRange(resources.AsNativeArray());
            }
        }
    }
}