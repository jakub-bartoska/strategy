using _Monobehaviors.ui;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.ui.marked
{
    public partial struct MarkedTownUiSystem : ISystem
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

            var townCompanies = new NativeList<ArmyCompany>(Allocator.TempJob);
            var markedTowns = new NativeList<TownTag>(Allocator.TempJob);

            new CollectMarkedTownsJob
                {
                    markedCompanies = townCompanies,
                    markedTowns = markedTowns
                }.Schedule(state.Dependency)
                .Complete();

            if (markedTowns.Length != 1) return;

            var toDeploy = new NativeList<ArmyCompany>(Allocator.TempJob);
            new CollectTownDeployerCompanies
                {
                    toDeploy = toDeploy
                }.Schedule(state.Dependency)
                .Complete();

            if (interfaceState.ValueRO.state != UIState.TOWN_UI)
            {
                TownUi.instance.changeActive(true);
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