using _Monobehaviors.ui.player_resources;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.buy_army;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.selection;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace system.strategy.town
{
    public partial struct ArmyPurchaseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<ArmyPurchase>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var armyPurchases = SystemAPI.GetSingletonBuffer<ArmyPurchase>();
            if (armyPurchases.IsEmpty) return;

            //checknout pocet existujicich armad
            var townCompanies = new NativeList<ArmyCompany>(Allocator.TempJob);
            new TownCompaniesCounterJob
                {
                    townCompanies = townCompanies
                }.Schedule(state.Dependency)
                .Complete();

            //todo prehodit do configu
            if (townCompanies.Length > 9)
            {
                armyPurchases.Clear();
                return;
            }

            var costs = new NativeList<ResourceHolder>(Allocator.TempJob);
            costs.Add(new ResourceHolder
            {
                type = ResourceType.FOOD,
                value = 2
            });
            costs.Add(new ResourceHolder
            {
                type = ResourceType.GOLD,
                value = 3
            });
            var idGenerator = SystemAPI.GetSingletonRW<IdGenerator>();
            new PurchaseNewArmyJob
                {
                    armyPurchases = armyPurchases.AsNativeArray(),
                    idGenerator = idGenerator,
                    armyCost = costs
                }.Schedule(state.Dependency)
                .Complete();

            armyPurchases.Clear();
        }
    }

    public partial struct TownCompaniesCounterJob : IJobEntity
    {
        public NativeList<ArmyCompany> townCompanies;

        private void Execute(Marked marked, TownTag townTag, ref DynamicBuffer<ArmyCompany> companies)
        {
            foreach (var company in companies)
            {
                townCompanies.Add(company);
            }
        }
    }

    public partial struct PurchaseNewArmyJob : IJobEntity
    {
        public NativeArray<ArmyPurchase> armyPurchases;
        [NativeDisableUnsafePtrRestriction] public RefRW<IdGenerator> idGenerator;
        public NativeList<ResourceHolder> armyCost;

        private void Execute(Marked marked, TownTag townTag, ref DynamicBuffer<ResourceHolder> resources, ref DynamicBuffer<ArmyCompany> companies)
        {
            foreach (var armyPurchase in armyPurchases)
            {
                if (!canPurchase(armyCost, resources, armyPurchase.count)) continue;

                for (int i = 0; i < resources.Length; i++)
                {
                    foreach (var costResource in armyCost)
                    {
                        if (resources[i].type != costResource.type) continue;

                        var resource = resources[i];
                        resource.value -= costResource.value * armyPurchase.count;
                        resources[i] = resource;
                    }
                }

                companies.Add(new ArmyCompany
                {
                    id = idGenerator.ValueRW.nextCompanyIdToBeUsed++,
                    type = armyPurchase.type,
                    soldierCount = armyPurchase.count
                });
            }
        }

        private bool canPurchase(NativeArray<ResourceHolder> cost, DynamicBuffer<ResourceHolder> resources, int soldierCount)
        {
            foreach (var costResource in cost)
            {
                foreach (var resourceHolder in resources)
                {
                    if (costResource.type != resourceHolder.type) continue;

                    if (costResource.value * soldierCount > resourceHolder.value) return false;
                }
            }

            return true;
        }
    }
}