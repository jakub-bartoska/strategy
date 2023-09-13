using component;
using component._common.camera;
using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.general;
using component.helpers;
using component.strategy._init_map;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.events;
using component.strategy.general;
using component.strategy.player_resources;
using system._common.army_to_spawn_switcher.common;
using system.strategy.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.strategy.spawner
{
    [UpdateAfter(typeof(AutoAddBlockersSystem))]
    [BurstCompile]
    public partial struct StrategySpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<TeamColor>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsArmySpawn(blockers)) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            setupSingleton(ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            ecb = new EntityCommandBuffer(Allocator.TempJob);

            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();
            var idGenerator = SystemAPI.GetSingletonRW<IdGenerator>();

            var armiesToSpawn =
                new NativeParallelMultiHashMap<SpawnArmy, SpawnArmyCompanyBuffer>(1000, Allocator.TempJob);
            new FetchArmiesToSpawnJob
                {
                    ecb = ecb,
                    armiesToSpawn = armiesToSpawn
                }.Schedule(state.Dependency)
                .Complete();

            var townsToSpawn =
                new NativeParallelMultiHashMap<SpawnTown, SpawnTownCompanyBuffer>(1000, Allocator.TempJob);
            new FetchTownsToSpawnJob
                {
                    ecb = ecb,
                    townsToSpawn = townsToSpawn
                }.Schedule(state.Dependency)
                .Complete();


            foreach (var army in getUniqueKeys(armiesToSpawn))
            {
                var companies = new NativeList<ArmyCompany>(Allocator.TempJob);
                foreach (var company in armiesToSpawn.GetValuesForKey(army))
                {
                    var newCompany = new ArmyCompany
                    {
                        id = idGenerator.ValueRW.nextCompanyIdToBeUsed++,
                        soldierCount = company.soldierCount,
                        type = company.type
                    };
                    companies.Add(newCompany);
                }

                SpawnUtils.spawnArmy(army.team, army.position, companies, ecb, prefabHolder, idGenerator, teamColors);
                companies.Dispose();
            }

            foreach (var town in getUniqueKeysForTown(townsToSpawn))
            {
                var companies = new NativeList<ArmyCompany>(Allocator.TempJob);
                foreach (var company in townsToSpawn.GetValuesForKey(town))
                {
                    var newCompany = new ArmyCompany
                    {
                        id = idGenerator.ValueRW.nextCompanyIdToBeUsed++,
                        soldierCount = company.soldierCount,
                        type = company.type
                    };
                    companies.Add(newCompany);
                }

                SpawnUtils.spawnTown(town.position, town.team, prefabHolder, ecb, idGenerator, companies);

                companies.Dispose();
            }


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private NativeArray<SpawnArmy> getUniqueKeys(NativeParallelMultiHashMap<SpawnArmy, SpawnArmyCompanyBuffer> map)
        {
            var keys = map.GetKeyArray(Allocator.TempJob);
            var uniqueCount = keys.Unique();
            return keys.GetSubArray(0, uniqueCount);
        }

        private NativeArray<SpawnTown> getUniqueKeysForTown(
            NativeParallelMultiHashMap<SpawnTown, SpawnTownCompanyBuffer> map)
        {
            var keys = map.GetKeyArray(Allocator.TempJob);
            var uniqueCount = keys.Unique();
            return keys.GetSubArray(0, uniqueCount);
        }

        private void setupSingleton(EntityCommandBuffer ecb)
        {
            var singletonEntity = ecb.CreateEntity();

            var idGenerator = new IdGenerator
            {
                nextIdToBeUsed = 1,
                nextCompanyIdToBeUsed = 1,
            };

            var goldHolder = new GoldHolder
            {
                gold = 1000,
                goldPerSecond = 6,
                timeRemaining = 1
            };
            var marker = new SelectionMarkerState
            {
                state = MarkerState.IDLE
            };
            var playerSettings = new GamePlayerSettings
            {
                playerTeam = Team.TEAM1
            };
            var interfaceState = new InterfaceState
            {
                state = UIState.ALL_CLOSED
            };
            var camera = new StrategyCamera
            {
                desiredPosition = new float3(-10, 10, -5)
            };

            ecb.AddComponent(singletonEntity, marker);
            ecb.AddComponent(singletonEntity, playerSettings);
            ecb.AddComponent(singletonEntity, interfaceState);
            ecb.AddComponent(singletonEntity, idGenerator);
            ecb.AddComponent(singletonEntity, goldHolder);
            ecb.AddComponent(singletonEntity, camera);
            ecb.AddComponent(singletonEntity, new StrategySingletonEntityTag());
            ecb.AddComponent(singletonEntity, new StrategyCleanupTag());

            ecb.AddBuffer<ArmyToSpawn>(singletonEntity);
            ecb.AddBuffer<CreateNewArmyEvent>(singletonEntity);
            ecb.AddBuffer<Damage>(singletonEntity);
            ecb.AddBuffer<CompanyMergeBuffer>(singletonEntity);
            ecb.AddBuffer<CompanyToDifferentState>(singletonEntity);
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.SPAWN_STRATEGY)
                {
                    containsArmySpawn = true;
                }
                else
                {
                    blockers.Add(blocker);
                }
            }

            return containsArmySpawn;
        }
    }

    [BurstCompile]
    public partial struct FetchArmiesToSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NativeParallelMultiHashMap<SpawnArmy, SpawnArmyCompanyBuffer> armiesToSpawn;

        private void Execute(SpawnArmy armyToSpawn, DynamicBuffer<SpawnArmyCompanyBuffer> companies, Entity oldEntity)
        {
            foreach (var company in companies)
            {
                armiesToSpawn.Add(armyToSpawn, company);
            }

            //during first spawn I have to remove mesh from map + add component again for restart
            var newEntity = ecb.CreateEntity();
            ecb.AddComponent(newEntity, armyToSpawn);
            var buffer = ecb.AddBuffer<SpawnArmyCompanyBuffer>(newEntity);
            buffer.AddRange(companies.ToNativeArray(Allocator.Temp));

            ecb.DestroyEntity(oldEntity);
        }
    }

    [BurstCompile]
    public partial struct FetchTownsToSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NativeParallelMultiHashMap<SpawnTown, SpawnTownCompanyBuffer> townsToSpawn;

        private void Execute(SpawnTown townToSpawn, DynamicBuffer<SpawnTownCompanyBuffer> companies, Entity oldEntity)
        {
            foreach (var company in companies)
            {
                townsToSpawn.Add(townToSpawn, company);
            }

            //during first spawn I have to remove mesh from map + add component again for restart
            var newEntity = ecb.CreateEntity();
            ecb.AddComponent(newEntity, townToSpawn);
            var buffer = ecb.AddBuffer<SpawnTownCompanyBuffer>(newEntity);
            buffer.AddRange(companies.ToNativeArray(Allocator.Temp));

            ecb.DestroyEntity(oldEntity);
        }
    }
}