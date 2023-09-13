using System;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.general;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace component.strategy.interactions.company
{
    public partial struct CompanyMergingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<CompanyMergeBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var mergeBuffer = SystemAPI.GetSingletonBuffer<CompanyMergeBuffer>();
            if (mergeBuffer.Length == 0) return;

            var companyPairsToMerge = mergeBuffer.ToNativeArray(Allocator.TempJob);

            //value is Company - armyId/townId
            var companyIdToCompanyMap = new NativeHashMap<long, (ArmyCompany, IdHolder)>(10, Allocator.TempJob);

            new FindCompaniesToMergeJob
                {
                    companyPairsToMerge = companyPairsToMerge,
                    companyIdToCompanyMap = companyIdToCompanyMap
                }.Schedule(state.Dependency)
                .Complete();

            var validPairs = new NativeList<(long, long)>(Allocator.TempJob);
            var affectedCompanyHolders = new NativeHashSet<long>(companyPairsToMerge.Length * 2, Allocator.TempJob);
            foreach (var companyMergeBuffer in companyPairsToMerge)
            {
                var company1 = companyIdToCompanyMap[companyMergeBuffer.companyId1];
                var company2 = companyIdToCompanyMap[companyMergeBuffer.companyId2];

                var hasSameHolder = company1.Item2.type switch
                {
                    HolderType.ARMY => company1.Item2.id == company2.Item2.id,
                    HolderType.TOWN => company1.Item2.id == company2.Item2.id ||
                                       company1.Item2.id + 1 == company2.Item2.id,
                    HolderType.TOWN_DEPLOYER => company1.Item2.id == company2.Item2.id ||
                                                company1.Item2.id - 1 == company2.Item2.id,
                    _ => throw new Exception("Unknown type")
                };

                if (!hasSameHolder) continue;
                if (company1.Item1.type != company2.Item1.type) continue;
                validPairs.Add((companyMergeBuffer.companyId1, companyMergeBuffer.companyId2));
                affectedCompanyHolders.Add(company1.Item2.id);
                if (company1.Item2.id != company2.Item2.id)
                {
                    affectedCompanyHolders.Add(company2.Item2.id);
                }
            }

            new ProcessCompanyMergeForArmyJob
                {
                    affectedArmies = affectedCompanyHolders,
                    companyPairsToMerge = validPairs,
                    companyIdToCompanyMap = companyIdToCompanyMap
                }.ScheduleParallel(state.Dependency)
                .Complete();

            mergeBuffer.Clear();
        }
    }

    [BurstCompile]
    public partial struct FindCompaniesToMergeJob : IJobEntity
    {
        public NativeArray<CompanyMergeBuffer> companyPairsToMerge;
        public NativeHashMap<long, (ArmyCompany, IdHolder)> companyIdToCompanyMap;

        private void Execute(DynamicBuffer<ArmyCompany> companies, IdHolder idHolder)
        {
            if (companies.Length == 0) return;

            foreach (var armyCompany in companies)
            {
                foreach (var valueTuple in companyPairsToMerge)
                {
                    if (armyCompany.id == valueTuple.companyId1)
                    {
                        companyIdToCompanyMap.Add(armyCompany.id, (armyCompany, idHolder));
                    }

                    if (armyCompany.id == valueTuple.companyId2)
                    {
                        companyIdToCompanyMap.Add(armyCompany.id, (armyCompany, idHolder));
                    }
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessCompanyMergeForArmyJob : IJobEntity
    {
        [ReadOnly] public NativeList<(long, long)> companyPairsToMerge;
        [ReadOnly] public NativeHashMap<long, (ArmyCompany, IdHolder)> companyIdToCompanyMap;
        [ReadOnly] public NativeHashSet<long> affectedArmies;

        private void Execute(ref DynamicBuffer<ArmyCompany> companies, IdHolder idHolder)
        {
            if (!affectedArmies.Contains(idHolder.id)) return;
            var oldCompanies = companies.ToNativeArray(Allocator.Temp);
            companies.Clear();
            foreach (var armyCompany in oldCompanies)
            {
                foreach (var valueTuple in companyPairsToMerge)
                {
                    if (armyCompany.id == valueTuple.Item1)
                    {
                        //destroy
                    }
                    else if (armyCompany.id == valueTuple.Item2)
                    {
                        var armyToMove = companyIdToCompanyMap[valueTuple.Item1].Item1;
                        var newArmyCompany = new ArmyCompany
                        {
                            soldierCount = armyCompany.soldierCount + armyToMove.soldierCount,
                            type = armyCompany.type,
                            id = armyCompany.id
                        };
                        companies.Add(newArmyCompany);
                    }
                    else
                    {
                        companies.Add(armyCompany);
                    }
                }
            }
        }
    }
}