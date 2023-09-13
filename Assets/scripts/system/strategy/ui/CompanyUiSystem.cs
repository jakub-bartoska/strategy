using System.Collections.Generic;
using _Monobehaviors.ui;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui
{
    public partial struct CompanyUiSystem : ISystem
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
                return;
            }

            var townCompanies = new NativeList<ArmyCompany>(Allocator.TempJob);
            var markedTowns = new NativeList<TownTag>(Allocator.TempJob);
            ;
            new CollectMarkedTownsJob
                {
                    markedCompanies = townCompanies,
                    markedTowns = markedTowns
                }.Schedule(state.Dependency)
                .Complete();

            if (markedTowns.Length != 0)
            {
                var toDeploy = new NativeList<ArmyCompany>(Allocator.TempJob);
                new CollectTownDeployerCompanies
                    {
                        toDeploy = toDeploy
                    }.Schedule(state.Dependency)
                    .Complete();
                if (interfaceState.ValueRO.state != UIState.TOWN_UI)
                {
                    TownUi.instance.changeActive(true);
                    TownUi.instance.displayTown(townCompanies.AsArray(), toDeploy.AsArray());
                    if (interfaceState.ValueRW.state == UIState.GET_NEW_STATE)
                    {
                        interfaceState.ValueRW.state = UIState.TOWN_UI;
                    }
                    else
                    {
                        interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
                        interfaceState.ValueRW.state = UIState.TOWN_UI;
                    }
                }

                TownUi.instance.displayTown(townCompanies.AsArray(), toDeploy.AsArray());
                return;
            }

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

    public partial struct CollectMarkedTownsJob : IJobEntity
    {
        public NativeList<ArmyCompany> markedCompanies;
        public NativeList<TownTag> markedTowns;

        private void Execute(TownTag tag, Marked marked, DynamicBuffer<ArmyCompany> companies)
        {
            markedTowns.Add(tag);
            markedCompanies.AddRange(companies.AsNativeArray());
        }
    }

    public partial struct CollectTownDeployerCompanies : IJobEntity
    {
        public NativeList<ArmyCompany> toDeploy;

        private void Execute(TownDeployerTag deployerTag, DynamicBuffer<ArmyCompany> companies, Marked marked)
        {
            toDeploy.AddRange(companies.AsNativeArray());
        }
    }
}