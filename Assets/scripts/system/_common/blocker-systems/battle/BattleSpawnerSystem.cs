using System;
using component;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.config.authoring_pairs;
using component.config.game_settings;
using component.general;
using component.soldier;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

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
            var defaultPositionOffset = new float3(10000, 0, 10000);
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsArmySpawn(blockers)) return;

            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var random = SystemAPI.GetSingletonRW<GameRandom>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var mapTransform = LocalTransform.FromPosition(defaultPositionOffset);
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

                var battalionPosition = getBattalionPosition(battalionToSpawn, defaultPositionOffset);

                var newBattalion = BattalionSpawner.spawnBattalion(ecb, battalionToSpawn, prefabHolder, battalionId++, defaultPositionOffset, battalionPosition);

                var battalionSoldiers = new NativeParallelHashSet<BattalionSoldiers>(battalionToSpawn.count, Allocator.TempJob);

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

                if (battalionToSpawn.team == Team.TEAM1)
                {
                    team1SoldierSum += battalionToSpawn.count;
                }
                else
                {
                    team2SoldierSum += battalionToSpawn.count;
                }
            }

            var singletonEntity = ecb.CreateEntity();

            ecb.AddComponent(singletonEntity, new BattleSingletonEntityTag());
            ecb.AddComponent(singletonEntity, new BattleCleanupTag());

            var battleSoldierCounts = new BattleSoldierCounts
            {
                team1Count = team1SoldierSum,
                team2Count = team2SoldierSum
            };
            ecb.AddComponent(singletonEntity, battleSoldierCounts);
            randomPerThread.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private float3 getBattalionPosition(BattalionToSpawn battalionToSpawn, float3 defaultPositionOffset)
        {
            var distanceFromMiddle = battalionToSpawn.team switch
            {
                Team.TEAM1 => 50,
                Team.TEAM2 => -50,
            };
            return new float3
            {
                x = battalionToSpawn.position.x * 5 + distanceFromMiddle + defaultPositionOffset.x,
                y = 0 + defaultPositionOffset.y,
                z = defaultPositionOffset.z + 40 - (battalionToSpawn.position.y * 10)
            };
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
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

        private NativeArray<Unity.Mathematics.Random> createRandomperThread(RefRW<GameRandom> random)
        {
            var randomPerThread =
                new NativeArray<Unity.Mathematics.Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);

            for (var i = 0; i < randomPerThread.Length; i++)
            {
                randomPerThread[i] = new Unity.Mathematics.Random((uint) random.ValueRW.random.NextInt());
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
    }


    [BurstCompile]
    public struct SpawnerJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [NativeDisableParallelForRestriction] public NativeArray<Unity.Mathematics.Random> randoms;
        public PrefabHolder prefabHolder;
        public Team team;
        public SoldierType soldierType;
        public int entityIndexAdd;
        public float4 teamColor;
        public long companyId;
        public float3 battalionPosition;
        public NativeParallelHashSet<BattalionSoldiers>.ParallelWriter battalionSoldiers;
        [NativeSetThreadIndex] private int threadIndex;

        [BurstCompile]
        public void Execute(int index)
        {
            Entity prefab;
            switch (soldierType)
            {
                case SoldierType.ARCHER:
                    prefab = prefabHolder.archerPrefab;
                    break;
                case SoldierType.SWORDSMAN:
                    prefab = prefabHolder.soldierPrefab;
                    break;
                default:
                    throw new Exception("unknown soldier type");
            }

            var newEntity = ecb.Instantiate(index, prefab);
            var calculatedPosition = getPosition(index, battalionPosition);
            var transform = LocalTransform.FromPosition(calculatedPosition);

            var soldierStats = new SoldierStatus
            {
                index = index + entityIndexAdd,
                team = team,
                companyId = companyId
            };
            var soldierHp = new SoldierHp
            {
                hp = 100
            };

            var color = new MaterialColorComponent
            {
                Value = teamColor
            };

            ecb.SetName(index, newEntity, "Soldeir " + soldierStats.index);

            //add components
            ecb.AddComponent(index, newEntity, soldierStats);
            ecb.AddComponent(index, newEntity, soldierHp);
            ecb.AddComponent(index, newEntity, color);
            ecb.AddComponent(index, newEntity, new BattleCleanupTag());

            //set component
            ecb.SetComponent(index, newEntity, transform);

            battalionSoldiers.Add(new BattalionSoldiers
            {
                soldierId = soldierStats.index,
                entity = newEntity
            });
        }

        private float3 getPosition(int index, float3 battalionPosition)
        {
            var soldierWithinBattalionPosition = new float3
            {
                x = 0,
                y = 0,
                z = index + 1
            };
            return battalionPosition + soldierWithinBattalionPosition;
        }
    }
}