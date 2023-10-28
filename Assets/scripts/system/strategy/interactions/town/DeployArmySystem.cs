using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.strategy.army_components;
using component.strategy.events;
using component.strategy.general;
using system.strategy.utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace component.strategy.interactions.town
{
    public partial struct DeployArmySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<CreateNewArmyEvent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var createEvents = SystemAPI.GetSingletonBuffer<CreateNewArmyEvent>();
            if (createEvents.Length == 0) return;

            var createArmyCompanyToGroup =
                new NativeParallelMultiHashMap<int, long>(createEvents.Length * 10, Allocator.TempJob);
            for (var i = 0; i < createEvents.Length; i++)
            {
                foreach (var company in createEvents[i].companiesToDeploy)
                {
                    createArmyCompanyToGroup.Add(i, company);
                }
            }

            var createEventArray = createEvents.ToNativeArray(Allocator.TempJob);

            var result = new NativeList<(Team, float3, int)>(createEventArray.Length, Allocator.TempJob);
            var companyIdToCompany = new NativeHashMap<long, ArmyCompany>(createEvents.Length * 10, Allocator.TempJob);
            new GetCompaniesFromDeployerJob
                {
                    createArmyCompanyToGroup = createArmyCompanyToGroup,
                    companyIdToCompany = companyIdToCompany,
                    result = result
                }.Schedule(state.Dependency)
                .Complete();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var idGenerator = SystemAPI.GetSingletonRW<IdGenerator>();
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();

            foreach (var (team, position, bufferIndex) in result)
            {
                var companies = new NativeList<ArmyCompany>(Allocator.TempJob);
                foreach (var company in createArmyCompanyToGroup.GetValuesForKey(bufferIndex))
                {
                    companies.Add(companyIdToCompany[company]);
                }

                ArmySpawner.spawnArmy(team, position, companies, ecb, prefabHolder, idGenerator, teamColors);
            }

            createEvents.Clear();
        }
    }

    public partial struct GetCompaniesFromDeployerJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, long> createArmyCompanyToGroup;
        public NativeHashMap<long, ArmyCompany> companyIdToCompany;
        public NativeList<(Team, float3, int)> result;

        private void Execute(ref DynamicBuffer<ArmyCompany> companiesBuffer, TeamComponent teamComponent,
            LocalTransform transform)
        {
            var indexesToRemove = new NativeList<int>(Allocator.Temp);
            var keys = createArmyCompanyToGroup.GetKeyArray(Allocator.Temp);

            for (var i = 0; i < keys.Length; i++)
            {
                var eventCompanies = createArmyCompanyToGroup.GetValuesForKey(keys[i]);
                foreach (var companyId in eventCompanies)
                {
                    for (var j = 0; j < companiesBuffer.Length; j++)
                    {
                        var armyCompany = companiesBuffer[j];
                        if (armyCompany.id == companyId)
                        {
                            companyIdToCompany.Add(companyId, armyCompany);
                            indexesToRemove.Add(j);
                        }
                    }
                }

                if (indexesToRemove.Length == 0) continue;

                indexesToRemove.Sort();

                for (var k = indexesToRemove.Length - 1; k >= 0; k--)
                {
                    companiesBuffer.RemoveAtSwapBack(indexesToRemove[k]);
                }

                indexesToRemove.Clear();
                result.Add((teamComponent.team, transform.Position, i));
            }
        }
    }
}