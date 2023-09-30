using _Monobehaviors.minor_ui;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.minor_objects;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked
{
    public partial struct MarkedMinorUiSystem : ISystem
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

            var markedMinors = new NativeList<MinorTag>(Allocator.TempJob);

            new CollectMarkedMinorsJob
                {
                    markedMinors = markedMinors
                }.Schedule(state.Dependency)
                .Complete();

            if (markedMinors.Length != 1) return;

            if (interfaceState.ValueRO.state != UIState.MINOR_UI)
            {
                MinorUi.instance.changeActive(true);
                if (interfaceState.ValueRW.state == UIState.GET_NEW_STATE)
                {
                    interfaceState.ValueRW.state = UIState.MINOR_UI;
                }
                else
                {
                    interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
                    interfaceState.ValueRW.state = UIState.MINOR_UI;
                }
            }
        }
    }

    public partial struct CollectMarkedMinorsJob : IJobEntity
    {
        public NativeList<MinorTag> markedMinors;

        private void Execute(MinorTag tag, Marked marked)
        {
            markedMinors.Add(tag);
        }
    }
}