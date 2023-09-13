using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace system.strategy.town
{
    public partial struct ArmySpawner : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<SoldierSpawner>();
            state.RequireForUpdate<IdGenerator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var idGenerator = SystemAPI.GetSingletonRW<IdGenerator>();
            new SpawnNewArmyJob
                {
                    deltaTime = deltaTime,
                    idGenerator = idGenerator
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    public partial struct SpawnNewArmyJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;
        [NativeDisableUnsafePtrRestriction] public RefRW<IdGenerator> idGenerator;

        private void Execute(ref SoldierSpawner spawner, ref DynamicBuffer<ArmyCompany> companies)
        {
            spawner.timeLeft -= deltaTime;

            if (spawner.timeLeft > 0) return;

            spawner.timeLeft += spawner.cycleTime;

            for (int i = 0; i < companies.Length; i++)
            {
                if (companies[i].type == spawner.soldierType)
                {
                    var company = companies[i];
                    company.soldierCount += spawner.soldiersAmountToSpawn;
                    companies[i] = company;
                    return;
                }
            }

            var newCompany = new ArmyCompany
            {
                soldierCount = spawner.soldiersAmountToSpawn,
                type = spawner.soldierType,
                id = idGenerator.ValueRW.nextCompanyIdToBeUsed++
            };
            companies.Add(newCompany);
        }
    }
}