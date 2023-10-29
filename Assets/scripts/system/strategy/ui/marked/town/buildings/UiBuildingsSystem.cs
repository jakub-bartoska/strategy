using component._common.system_switchers;
using component.strategy.army_components.ui;
using Unity.Burst;
using Unity.Entities;

namespace system.strategy.ui.marked.town.buildings
{
    public partial struct UiBuildingsSystem : ISystem
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
            var interfaceState = SystemAPI.GetSingletonRW<InterfaceState>();
            if (interfaceState.ValueRO.state != UIState.TOWN_BUILDINGS_UI) return;
        }
    }
}