using _Monobehaviors.resource;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked.resource
{
    public partial struct ResourceUiUpdater : ISystem
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
            var markedCaravanResources = new NativeList<ResourceHolder>(100, Allocator.TempJob);
            var idHolders = new NativeList<IdHolder>(100, Allocator.TempJob);
            new CollectMarkedResources
                {
                    idHolders = idHolders,
                    markedCaravanResources = markedCaravanResources
                }.Schedule(state.Dependency)
                .Complete();

            var type = getResourceState(idHolders);
            ResourceInfoHolder.instance.updateState(markedCaravanResources, type);
        }

        private ResourceTabState getResourceState(NativeList<IdHolder> idHolders)
        {
            if (idHolders.Length != 1)
            {
                return ResourceTabState.CLOSED;
            }

            return ResourceTabState.OPEN;
        }

        public partial struct CollectMarkedResources : IJobEntity
        {
            public NativeList<ResourceHolder> markedCaravanResources;
            public NativeList<IdHolder> idHolders;

            private void Execute(Marked marked, DynamicBuffer<ResourceHolder> resources, IdHolder idHolder)
            {
                idHolders.Add(idHolder);
                markedCaravanResources.AddRange(resources.AsNativeArray());
            }
        }
    }
}