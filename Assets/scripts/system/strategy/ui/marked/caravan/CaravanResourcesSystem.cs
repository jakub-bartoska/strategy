using _Monobehaviors.resource;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.caravan;
using component.strategy.player_resources;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui
{
    public partial struct CaravanResourcesSystem : ISystem
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
            if (interfaceState.state != UIState.CARAVAN_UI) return;

            var markedCaravanResources = new NativeList<ResourceHolder>(10, Allocator.TempJob);
            new CollectMarkedCaravanResources
                {
                    markedCaravanResources = markedCaravanResources
                }.Schedule(state.Dependency)
                .Complete();

            CaravanResource.instance.updateResources(markedCaravanResources);
        }

        public partial struct CollectMarkedCaravanResources : IJobEntity
        {
            public NativeList<ResourceHolder> markedCaravanResources;

            private void Execute(CaravanTag tag, Marked marked, DynamicBuffer<ResourceHolder> resources)
            {
                markedCaravanResources.AddRange(resources.AsNativeArray());
            }
        }
    }
}