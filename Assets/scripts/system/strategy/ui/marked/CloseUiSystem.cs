using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.selection;
using Unity.Burst;
using Unity.Entities;

namespace system.strategy.ui.marked
{
    public partial struct CloseUiSystem : ISystem
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
            if (interfaceState.ValueRO.state == UIState.ALL_CLOSED)
            {
                return;
            }

            //firstly process old state
            if (interfaceState.ValueRO.state != interfaceState.ValueRO.oldState &&
                interfaceState.ValueRO.state != UIState.GET_NEW_STATE)
            {
                return;
            }

            var markedEntitiesCount = SystemAPI.QueryBuilder()
                .WithAll<Marked>()
                .Build()
                .CalculateEntityCount();

            if (markedEntitiesCount > 0) return;

            if (interfaceState.ValueRW.state == UIState.GET_NEW_STATE)
            {
                interfaceState.ValueRW.state = UIState.ALL_CLOSED;
            }
            else
            {
                interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
                interfaceState.ValueRW.state = UIState.ALL_CLOSED;
            }
        }
    }
}