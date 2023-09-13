using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace component.strategy.interactions.company
{
    public partial struct CompanyToDifferentStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<CompanyToDifferentState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var companyToDifferentStates = SystemAPI.GetSingletonBuffer<CompanyToDifferentState>();
            if (companyToDifferentStates.Length == 0) return;

            var companiesToMove =
                new NativeHashMap<long, ArmyCompany>(companyToDifferentStates.Length, Allocator.TempJob);

            new RemoveCompaniesJob
                {
                    companyToDifferentStates = companyToDifferentStates,
                    companiesToMove = companiesToMove
                }.Schedule(state.Dependency)
                .Complete();

            new AddCompaniesToTown
                {
                    companyToDifferentStates = companyToDifferentStates,
                    companiesToMove = companiesToMove
                }.Schedule(state.Dependency)
                .Complete();

            new AddCompaniesToDeploy
                {
                    companyToDifferentStates = companyToDifferentStates,
                    companiesToMove = companiesToMove
                }.Schedule(state.Dependency)
                .Complete();

            companyToDifferentStates.Clear();
        }
    }

    [BurstCompile]
    public partial struct RemoveCompaniesJob : IJobEntity
    {
        [ReadOnly] public DynamicBuffer<CompanyToDifferentState> companyToDifferentStates;
        public NativeHashMap<long, ArmyCompany> companiesToMove;

        private void Execute(DynamicBuffer<ArmyCompany> companies, Marked marked)
        {
            var indexesToRemove = new NativeList<int>(Allocator.Temp);
            for (var i = 0; i < companies.Length; i++)
            {
                foreach (var companyToDifferentState in companyToDifferentStates)
                {
                    if (companies[i].id == companyToDifferentState.companyId)
                    {
                        indexesToRemove.Add(i);
                        companiesToMove.Add(companies[i].id, companies[i]);
                        break;
                    }
                }
            }

            for (var i = indexesToRemove.Length - 1; i >= 0; i--)
            {
                companies.RemoveAtSwapBack(indexesToRemove[i]);
            }
        }
    }

    [BurstCompile]
    public partial struct AddCompaniesToTown : IJobEntity
    {
        [ReadOnly] public DynamicBuffer<CompanyToDifferentState> companyToDifferentStates;
        [ReadOnly] public NativeHashMap<long, ArmyCompany> companiesToMove;

        private void Execute(TownTag townTag, DynamicBuffer<ArmyCompany> companies, Marked marked)
        {
            foreach (var companyToDifferentState in companyToDifferentStates)
            {
                if (companyToDifferentState.targetState != CompanyState.TOWN) continue;

                companies.Add(companiesToMove[companyToDifferentState.companyId]);
            }
        }
    }

    [BurstCompile]
    public partial struct AddCompaniesToDeploy : IJobEntity
    {
        [ReadOnly] public DynamicBuffer<CompanyToDifferentState> companyToDifferentStates;
        [ReadOnly] public NativeHashMap<long, ArmyCompany> companiesToMove;

        private void Execute(TownDeployerTag deployerTag, DynamicBuffer<ArmyCompany> companies, Marked marked)
        {
            foreach (var companyToDifferentState in companyToDifferentStates)
            {
                if (companyToDifferentState.targetState != CompanyState.TOWN_TO_DEPLOY) continue;

                companies.Add(companiesToMove[companyToDifferentState.companyId]);
            }
        }
    }
}