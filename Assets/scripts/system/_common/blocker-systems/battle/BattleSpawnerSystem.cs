using System;
using component;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.battle.config;
using component.config.authoring_pairs;
using component.config.game_settings;
using component.general;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace system
{
    [BurstCompile]
    public partial struct BattleSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<GameRandom>();
            state.RequireForUpdate<CompanyToSpawn>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsArmySpawn(blockers)) return;

            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var random = SystemAPI.GetSingletonRW<GameRandom>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var mapTransform = CustomTransformUtils.getMapTransform();
            mapTransform.Rotation = quaternion.RotateY(math.PI / 2);
            var map = ecb.Instantiate(prefabHolder.battleMapPrefab);
            ecb.SetName(map, "Map");
            ecb.AddComponent(map, new BattleCleanupTag());
            ecb.SetComponent(map, mapTransform);

            var randomPerThread = createRandomperThread(random);

            var team1SoldierSum = 0;
            var team2SoldierSum = 0;
            var battalionId = 0;
            foreach (var battalionToSpawn in battalionsToSpawn)
            {
                if (battalionToSpawn.count == 0)
                {
                    continue;
                }

                var battalionPosition =
                    CustomTransformUtils.getBattalionPositionForSoldiers(battalionToSpawn.position.x,
                        battalionToSpawn.position.y);

                var newBattalion = BattalionSpawner.spawnBattalion(ecb, battalionToSpawn, prefabHolder, battalionId++);

                var battalionSoldiers =
                    new NativeParallelHashSet<BattalionSoldiers>(battalionToSpawn.count, Allocator.TempJob);

                new SpawnerJob
                    {
                        ecb = ecb.AsParallelWriter(),
                        randoms = randomPerThread,
                        prefabHolder = prefabHolder,
                        team = battalionToSpawn.team,
                        soldierType = battalionToSpawn.armyType,
                        entityIndexAdd = (team1SoldierSum + team2SoldierSum),
                        teamColor = getColor(battalionToSpawn.team, teamColors),
                        companyId = battalionToSpawn.armyCompanyId,
                        battalionPosition = battalionPosition,
                        battalionSoldiers = battalionSoldiers.AsParallelWriter()
                    }.Schedule(battalionToSpawn.count, 128)
                    .Complete();

                var buffer = ecb.AddBuffer<BattalionSoldiers>(newBattalion);
                foreach (var battalionSoldier in battalionSoldiers)
                {
                    buffer.Add(battalionSoldier);
                }

                var battalionHealth = new BattalionHealth
                {
                    value = battalionToSpawn.count * 10
                };
                ecb.AddComponent(newBattalion, battalionHealth);

                if (battalionToSpawn.team == Team.TEAM1)
                {
                    team1SoldierSum += battalionToSpawn.count;
                }
                else
                {
                    team2SoldierSum += battalionToSpawn.count;
                }

                battalionSoldiers.Dispose();
            }

            var singletonEntity = ecb.CreateEntity();

            var battalionIdHolder = new BattalionIdHolder
            {
                nextBattalionId = battalionId
            };

            var movementDataHolder = new MovementDataHolder
            {
                blockers = new(1000, Allocator.Persistent),
                battalionFollowers = new(1000, Allocator.Persistent),
                inFightMovement = new(1000, Allocator.Persistent),
                plannedMovementDirections = new(1000, Allocator.Persistent),
                movingBattalions = new(1000, Allocator.Persistent),
                battalionExactDistance = new(1000, Allocator.Persistent),
                waitingForSoldiersBattalions = new(1000, Allocator.Persistent),
            };

            var allRowIds = new NativeList<int>(10, Allocator.Persistent);

            if (allRowIds.IsEmpty)
            {
                for (int i = 0; i < 10; i++)
                {
                    allRowIds.Add(i);
                }
            }

            var dataHolder = new DataHolder
            {
                allRowIds = allRowIds,
                allBattalionIds = new(1000, Allocator.Persistent),
                positions = new(1000, Allocator.Persistent),
                battalionInfo = new(1000, Allocator.Persistent),
                fightingPairs = new(1000, Allocator.Persistent),
                fightingBattalions = new(1000, Allocator.Persistent),
                battalionsPerformingAction = new(1000, Allocator.Persistent),
                needReinforcements = new(1000, Allocator.Persistent),
                reinforcements = new(1000, Allocator.Persistent),
                declinedReinforcements = new(1000, Allocator.Persistent),
                rowChanges = new(10, Allocator.Persistent),
                battalionSwitchRowDirections = new(1000, Allocator.Persistent),
                blockedHorizontalSplits = new(1000, Allocator.Persistent),
                splitBattalions = new(1000, Allocator.Persistent),
            };

            var backupPlanDataHolder = new BackupPlanDataHolder
            {
                battleChunksPerRowTeam = new(1000, Allocator.Persistent),
                emptyChunks = new(1000, Allocator.Persistent),
                moveLeft = new(1000, Allocator.Persistent),
                moveRight = new(1000, Allocator.Persistent),
                moveToDifferentChunk = new(1000, Allocator.Persistent),
                allChunks = new(1000, Allocator.Persistent),
                chunkLinks = new(1000, Allocator.Persistent),
                chunksNeedingReinforcements = new(1000, Allocator.Persistent),
                chunkReinforcementPaths = new(1000, Allocator.Persistent),
                battalionIdToChunk = new(1000, Allocator.Persistent),
                lastChunkId = 0
            };

            var soldierPositions = new SoldierPositions
            {
                positions = preparePosition()
            };

            var config = DebugConfigAuthoring.instance.collectData();

            ecb.AddComponent(singletonEntity, config);
            ecb.AddComponent(singletonEntity, battalionIdHolder);
            ecb.AddComponent(singletonEntity, new BattleSingletonEntityTag());
            ecb.AddComponent(singletonEntity, new BattleCleanupTag());
            ecb.AddComponent(singletonEntity, movementDataHolder);
            ecb.AddComponent(singletonEntity, dataHolder);
            ecb.AddComponent(singletonEntity, backupPlanDataHolder);
            ecb.AddComponent(singletonEntity, soldierPositions);

            randomPerThread.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.SPAWN_ARMY)
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

        private NativeArray<Random> createRandomperThread(RefRW<GameRandom> random)
        {
            var randomPerThread =
                new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);

            for (var i = 0; i < randomPerThread.Length; i++)
            {
                randomPerThread[i] = new Random((uint)random.ValueRW.random.NextInt());
            }

            return randomPerThread;
        }

        private float4 getColor(Team team, DynamicBuffer<TeamColor> colors)
        {
            foreach (var color in colors)
            {
                if (color.team == team)
                    return color.color;
            }

            throw new Exception("unknown team");
        }

        private NativeHashMap<int, NativeList<int>> preparePosition()
        {
            var positions = new NativeHashMap<int, NativeList<int>>(9, Allocator.Persistent);
            positions.Add(1, createPositonList(4));
            positions.Add(2, createPositonList(0, 9));
            positions.Add(3, createPositonList(0, 4, 9));
            positions.Add(4, createPositonList(0, 3, 6, 9));
            positions.Add(5, createPositonList(0, 2, 4, 7, 9));
            positions.Add(6, createPositonList(0, 2, 4, 5, 7, 9));
            positions.Add(7, createPositonList(0, 1, 3, 4, 6, 8, 9));
            positions.Add(8, createPositonList(0, 1, 3, 4, 5, 6, 8, 9));
            positions.Add(9, createPositonList(0, 1, 2, 3, 5, 6, 7, 8, 9));
            positions.Add(10, createPositonList(0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            return positions;
        }

        private NativeList<int> createPositonList(params int[] positions)
        {
            var result = new NativeList<int>(10, Allocator.Persistent);
            foreach (var position in positions)
            {
                result.Add(position);
            }

            return result;
        }
    }

    [BurstCompile]
    public struct SpawnerJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [NativeDisableParallelForRestriction] public NativeArray<Random> randoms;
        public PrefabHolder prefabHolder;
        public Team team;
        public SoldierType soldierType;
        public int entityIndexAdd;
        public float4 teamColor;
        public long companyId;
        public float3 battalionPosition;

        public NativeParallelHashSet<BattalionSoldiers>.ParallelWriter battalionSoldiers;

        //not used but save for later
        [NativeSetThreadIndex] private int threadIndex;

        [BurstCompile]
        public void Execute(int index)
        {
            SoldierSpawner.spawnSoldier(soldierType, prefabHolder, ecb, index, entityIndexAdd, companyId, team,
                battalionPosition, battalionSoldiers, teamColor);
        }
    }
}