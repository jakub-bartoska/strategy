using System.Collections.Generic;
using _Monobehaviors.ui;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked
{
    public partial struct MarkedArmyUiSystem : ISystem
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

            var markedCompanies = new NativeList<ArmyCompany>(Allocator.TempJob);
            new CollectMarkedArmiesJob
                {
                    markedCompanies = markedCompanies
                }.Schedule(state.Dependency)
                .Complete();

            if (markedCompanies.Length != 0)
            {
                if (interfaceState.ValueRO.state != UIState.ARMY_UI)
                {
                    CompaniesPanel.instance.changeActive(true);
                    if (interfaceState.ValueRW.state == UIState.GET_NEW_STATE)
                    {
                        interfaceState.ValueRW.state = UIState.ARMY_UI;
                    }
                    else
                    {
                        interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
                        interfaceState.ValueRW.state = UIState.ARMY_UI;
                    }
                }

                markedCompanies.Sort(new ArmyCompanySorter());

                CompaniesPanel.instance.displayCompanies(markedCompanies.AsArray());
            }
        }
    }

    public class ArmyCompanySorter : IComparer<ArmyCompany>
    {
        public int Compare(ArmyCompany e1, ArmyCompany e2)
        {
            return e1.id.CompareTo(e2.id);
        }
    }

    public partial struct CollectMarkedArmiesJob : IJobEntity
    {
        public NativeList<ArmyCompany> markedCompanies;

        private void Execute(ArmyTag tag, DynamicBuffer<ArmyCompany> companies, Marked marked)
        {
            markedCompanies.AddRange(companies.AsNativeArray());
        }
    }
}