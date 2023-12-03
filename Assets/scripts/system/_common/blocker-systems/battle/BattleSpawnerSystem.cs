using System;
using component;
using component._common.camera;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.authoring_pairs;
using component.config.game_settings;
using component.formation;
using component.general;
using component.helpers.positioning;
using component.pathfinding;
using component.soldier;
using component.soldier.behavior.behaviors;
using component.soldier.behavior.behaviors.shoot_arrow;
using component.soldier.behavior.fight;
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
            state.RequireForUpdate<ArmyToSpawn>();
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

            var teamPositions = SystemAPI.GetSingletonBuffer<TeamPositions>();
            var armiesToSpawn = SystemAPI.GetSingletonBuffer<ArmyToSpawn>();
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var random = SystemAPI.GetSingletonRW<GameRandom>();
            var arrowConfig = SystemAPI.GetSingleton<ArrowConfig>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var mapTransform = LocalTransform.FromPosition(new float3(10000, 0, 10000));
            var map = ecb.Instantiate(prefabHolder.battleMapPrefab);
            ecb.SetName(map, "Map");
            ecb.AddComponent(map, new BattleCleanupTag());
            ecb.SetComponent(map, mapTransform);

            var randomPerThread = createRandomperThread(random);

            var team1SoldierSum = 0;
            var team2SoldierSum = 0;
            foreach (var armyToSpawn in armiesToSpawn)
            {
                if (armyToSpawn.count == 0)
                {
                    continue;
                }

                var teamPosition = getTeamPositions(armyToSpawn.team, teamPositions);

                new SpawnerJob
                    {
                        ecb = ecb.AsParallelWriter(),
                        arrowConfig = arrowConfig,
                        randoms = randomPerThread,
                        prefabHolder = prefabHolder,
                        team = armyToSpawn.team,
                        soldierType = armyToSpawn.armyType,
                        entityIndexAdd = (team1SoldierSum + team2SoldierSum),
                        teamPosition = teamPosition,
                        teamColor = getColor(armyToSpawn.team, teamColors),
                        armyToSpawn = armyToSpawn,
                        companyId = armyToSpawn.armyCompanyId
                    }.Schedule(armyToSpawn.count, 128)
                    .Complete();

                if (armyToSpawn.team == Team.TEAM1)
                {
                    team1SoldierSum += armyToSpawn.count;
                }
                else
                {
                    team2SoldierSum += armyToSpawn.count;
                }
            }

            var singletonEntity = ecb.CreateEntity();
            var squarePositions = initSquarePositions(new float3(10090, 0, 10050), new float3(-10090, 0, -10050));
            ecb.AddComponent(singletonEntity, squarePositions);
            var positionHolder = new PositionHolder();
            positionHolder.soldierIdPosition =
                new NativeParallelMultiHashMap<int, float3>(team1SoldierSum + team2SoldierSum, Allocator.Persistent);
            positionHolder.team1PositionCells =
                new NativeParallelMultiHashMap<int2, int>(team1SoldierSum, Allocator.Persistent);
            positionHolder.team2PositionCells =
                new NativeParallelMultiHashMap<int2, int>(team2SoldierSum, Allocator.Persistent);
            var camera = new BattleCamera()
            {
                desiredPosition = new float3(10000, 100, 9950)
            };
            var formationManager = new FormationManager {maxFormationId = 0};

            ecb.AddComponent(singletonEntity, camera);
            ecb.AddComponent(singletonEntity, squarePositions);
            ecb.AddComponent(singletonEntity, positionHolder);
            ecb.AddComponent(singletonEntity, formationManager);
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

        private PositionHolderConfig initSquarePositions(float3 maxPosition, float3 minPosition)
        {
            return new PositionHolderConfig
            {
                oneSquareSize = 5,
                minSquarePosition = new int2((int) minPosition.x, (int) minPosition.z),
                maxSquarePosition = new int2((int) (maxPosition.x + 0.9f), (int) (maxPosition.z + 0.9f)),
            };
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

        private TeamPositions getTeamPositions(Team team, DynamicBuffer<TeamPositions> positions)
        {
            foreach (var position in positions)
            {
                if (position.team == team)
                {
                    return position;
                }
            }

            throw new Exception("unknown team");
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
        [NativeSetThreadIndex] private int threadIndex;
        public EntityCommandBuffer.ParallelWriter ecb;
        [NativeDisableParallelForRestriction] public NativeArray<Unity.Mathematics.Random> randoms;
        public PrefabHolder prefabHolder;
        public Team team;
        public SoldierType soldierType;
        public int entityIndexAdd;
        public TeamPositions teamPosition;
        public float4 teamColor;
        public ArrowConfig arrowConfig;
        public ArmyToSpawn armyToSpawn;
        public long companyId;

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
            var calculatedPosition = getPosition(index);
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
            var closestEnemy = new ClosestEnemy
            {
                distanceFromClosestEnemy = 0f,
                status = ClosestEnemyStatus.NO_ENEMY
            };

            var color = new MaterialColorComponent
            {
                Value = teamColor
            };

            var pathTracker = new PathTracker
            {
                timerRemaining = 0.3f,
                defaultTimer = 0.3f,
                isMoving = true
            };

            var soldierFormationStatus = new SoldierFormationStatus
            {
                formationStatus = FormationStatus.NO_FORMATION,
                formationId = 0
            };

            ecb.SetName(index, newEntity, "Soldeir " + soldierStats.index);

            //add components
            ecb.AddComponent(index, newEntity, pathTracker);
            ecb.AddComponent(index, newEntity, soldierStats);
            ecb.AddComponent(index, newEntity, soldierHp);
            ecb.AddComponent(index, newEntity, closestEnemy);
            ecb.AddComponent(index, newEntity, color);
            ecb.AddComponent(index, newEntity, soldierFormationStatus);
            ecb.AddComponent(index, newEntity, new BattleCleanupTag());

            //set component
            ecb.SetComponent(index, newEntity, transform);

            var fightContext = new FightContext
            {
                attackDelay = 0.2f,
                attackTimeRemaining = 0.2f
            };

            var shootArrowContext = new ShootArrowContext
            {
                shootTimeRemaining = arrowConfig.arrowShootingDelay,
                shootDelay = arrowConfig.arrowShootingDelay
            };

            var behaviorContext = new BehaviorContext();
            behaviorContext.behaviorToBeFinished = BehaviorType.NONE;
            behaviorContext.currentBehavior = BehaviorType.IDLE;
            behaviorContext.possibleBehaviors = new UnsafeList<BehaviorType>(5, Allocator.Persistent);
            behaviorContext.possibleBehaviors.Add(BehaviorType.FOLLOW_CLOSEST_ENEMY);
            behaviorContext.possibleBehaviors.Add(BehaviorType.IDLE);

            switch (soldierType)
            {
                case SoldierType.ARCHER:
                    ecb.AddComponent(index, newEntity, shootArrowContext);
                    behaviorContext.possibleBehaviors.Add(BehaviorType.SHOOT_ARROW);
                    break;
                case SoldierType.SWORDSMAN:
                    ecb.AddComponent(index, newEntity, fightContext);
                    behaviorContext.possibleBehaviors.Add(BehaviorType.FIGHT);
                    behaviorContext.possibleBehaviors.Add(BehaviorType.FOLLOW_CLOSEST_ENEMY);
                    behaviorContext.possibleBehaviors.Add(BehaviorType.MAKE_LINE_FORMATION);
                    behaviorContext.possibleBehaviors.Add(BehaviorType.PROCESS_FORMATION_COMMAND);
                    break;
                case SoldierType.HORSEMAN:
                    throw new NotImplementedException("implement me");
                default:
                    throw new Exception("unknown enum");
            }

            ecb.AddComponent(index, newEntity, behaviorContext);
        }

        private float3 getPosition(int index)
        {
            if (armyToSpawn.formation == Formation.NO_FORMATION)
            {
                var random = randoms[threadIndex];
                var position = new float3(
                    random.NextFloat(teamPosition.min.x, teamPosition.max.x),
                    0,
                    random.NextFloat(teamPosition.min.y, teamPosition.max.y)
                );
                randoms[threadIndex] = random;
                return position;
            }

            if (armyToSpawn.formation == Formation.LINE)
            {
                var xPosition = (teamPosition.min.x + teamPosition.max.x) / 2;
                var zMiddle = (teamPosition.min.y + teamPosition.max.y) / 2;
                var zStart = zMiddle - (armyToSpawn.count / 2f * armyToSpawn.distanceBetweenSoldiers);
                var zPosition = zStart + index * armyToSpawn.distanceBetweenSoldiers;
                return new float3(
                    xPosition,
                    0,
                    zPosition
                );
            }

            throw new Exception();
        }
    }
}