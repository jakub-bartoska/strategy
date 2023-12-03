using System.Collections.Generic;
using _Monobehaviors.ui;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.general;
using component.strategy.player_resources;
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
            var resources = new NativeList<ResourceHolder>(Allocator.TempJob);
            var armyIds = new NativeList<long>(Allocator.TempJob);
            new CollectMarkedArmiesJob
                {
                    markedCompanies = markedCompanies,
                    resources = resources,
                    armyIds = armyIds
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
        public NativeList<ResourceHolder> resources;
        public NativeList<long> armyIds;

        private void Execute(ArmyTag tag, DynamicBuffer<ArmyCompany> companies, Marked marked, DynamicBuffer<ResourceHolder> resourceBuffer, IdHolder idHolder)
        {
            armyIds.Add(idHolder.id);
            resources.AddRange(resourceBuffer.AsNativeArray());
            markedCompanies.AddRange(companies.AsNativeArray());
        }
    }
}