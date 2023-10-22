using _Monobehaviors.minor_ui;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.caravan;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked
{
    public partial struct MarkedCaravanUiSystem : ISystem
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

            var markedCaravans = new NativeList<CaravanTag>(Allocator.TempJob);

            new CollectMarkedCaravansJob
                {
                    markedCaravans = markedCaravans
                }.Schedule(state.Dependency)
                .Complete();

            if (markedCaravans.Length != 1) return;

            if (interfaceState.ValueRO.state != UIState.CARAVAN_UI)
            {
                CaravanUi.instance.changeActive(true);
                if (interfaceState.ValueRW.state == UIState.GET_NEW_STATE)
                {
                    interfaceState.ValueRW.state = UIState.CARAVAN_UI;
                }
                else
                {
                    interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
                    interfaceState.ValueRW.state = UIState.CARAVAN_UI;
                }
            }
        }
    }

    public partial struct CollectMarkedCaravansJob : IJobEntity
    {
        public NativeList<CaravanTag> markedCaravans;

        private void Execute(CaravanTag tag, Marked marked)
        {
            markedCaravans.Add(tag);
        }
    }
}