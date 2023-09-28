using _Monobehaviors.resource;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui
{
    public partial struct TownResources : ISystem
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
            if (interfaceState.state != UIState.TOWN_UI) return;

            var markedTownResources = new NativeList<ResourceHolder>(10, Allocator.TempJob);
            new CollectMArkedTownResources
                {
                    markedTownResources = markedTownResources
                }.Schedule(state.Dependency)
                .Complete();

            TownResource.instance.updateResources(markedTownResources);
        }

        public partial struct CollectMArkedTownResources : IJobEntity
        {
            public NativeList<ResourceHolder> markedTownResources;

            private void Execute(TownTag deployerTag, DynamicBuffer<ResourceHolder> companies, Marked marked)
            {
                markedTownResources.AddRange(companies.AsNativeArray());
            }
        }
    }
}