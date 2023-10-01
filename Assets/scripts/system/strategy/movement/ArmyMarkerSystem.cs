using System;
using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.strategy.army_components.ui;
using component.strategy.general;
using component.strategy.selection;
using system.strategy.ui;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.movement
{
    [BurstCompile]
    public partial struct ArmyMarkerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePlayerSettings>();
            state.RequireForUpdate<SelectionMarkerState>();
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var marker = SystemAPI.GetSingletonRW<SelectionMarkerState>();
            var interfaceState = SystemAPI.GetSingletonRW<InterfaceState>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            switch (marker.ValueRO.state)
            {
                case MarkerState.RUNNING:
                    return;
                case MarkerState.IDLE:
                    return;
                case MarkerState.FINISHED:
                    new UiCloserSystem.RemoveOldMarkersJob
                        {
                            ecb = ecb.AsParallelWriter()
                        }.Schedule(state.Dependency)
                        .Complete();
                    new UiCloserSystem.RemoveOldMarkersVisualJob
                        {
                            ecb = ecb.AsParallelWriter()
                        }.ScheduleParallel(state.Dependency)
                        .Complete();
                    markProperEntities(marker, ecb, state);
                    interfaceState.ValueRW.oldState = interfaceState.ValueRW.state;
                    interfaceState.ValueRW.state = UIState.GET_NEW_STATE;
                    marker.ValueRW.state = MarkerState.IDLE;
                    ecb.Playback(state.EntityManager);
                    ecb.Dispose();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// If there is at least 1 army, mark armies only.
        /// If there is exactly 1 town, mark this 1 town.
        /// If there is exactly 1 minor, mark minor.
        /// </summary>
        private void markProperEntities(RefRW<SelectionMarkerState> marker, EntityCommandBuffer ecb, SystemState state)
        {
            var markerPrefab = SystemAPI.GetSingleton<PrefabHolder>().markerPrefab;
            var playerSettings = SystemAPI.GetSingleton<GamePlayerSettings>();
            var entitiesCounts = new NativeArray<long>(3, Allocator.TempJob);
            //count entities in mark rectange
            new MarkEntitiesJob
                {
                    markerState = marker.ValueRO,
                    gamePlayerSettings = playerSettings,
                    ecb = ecb.AsParallelWriter(),
                    markerprefab = markerPrefab,
                    entitiesCounts = entitiesCounts,
                }.Schedule(state.Dependency)
                .Complete();
            //at least 1 army
            var shouldMarkArmy = entitiesCounts[0] > 0;
            //army should not be marked + exactly 1 town
            var shouldMarkTown = !shouldMarkArmy && entitiesCounts[1] == 1;
            //army and town should not be marked + exactly 1 town
            var shouldMarkMinor = !shouldMarkArmy && !shouldMarkTown && entitiesCounts[2] == 1;
            new MarkEntitiesJob
                {
                    markerState = marker.ValueRO,
                    gamePlayerSettings = playerSettings,
                    ecb = ecb.AsParallelWriter(),
                    markerprefab = markerPrefab,
                    entitiesCounts = entitiesCounts,
                    shouldMarkArmy = shouldMarkArmy,
                    shouldMarkTown = shouldMarkTown,
                    shouldMarkMinor = shouldMarkMinor
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct MarkEntitiesJob : IJobEntity
    {
        [ReadOnly] public SelectionMarkerState markerState;
        [ReadOnly] public GamePlayerSettings gamePlayerSettings;
        public EntityCommandBuffer.ParallelWriter ecb;

        public Entity markerprefab;

        // id 0 - army count
        // id 1 - town count
        // id 2 - minor count
        public NativeArray<long> entitiesCounts;
        public bool shouldMarkArmy;
        public bool shouldMarkTown;
        public bool shouldMarkMinor;

        private void Execute(LocalTransform transform, MarkableEntity markableEntity, Entity entity, TeamComponent team,
            IdHolder idHolder)
        {
            if (gamePlayerSettings.playerTeam != team.team)
            {
                return;
            }

            var minX = math.min(markerState.min.x, markerState.max.x);
            var manX = math.max(markerState.min.x, markerState.max.x);
            var minZ = math.min(markerState.min.z, markerState.max.z);
            var manZ = math.max(markerState.min.z, markerState.max.z);


            if (transform.Position.x > minX && transform.Position.x < manX &&
                transform.Position.z > minZ && transform.Position.z < manZ)
            {
                switch (idHolder.type)
                {
                    case HolderType.ARMY:
                        entitiesCounts[0] += 1;
                        if (shouldMarkArmy)
                        {
                            ecb.AddComponent(entity.Index + 10000, entity, new Marked());
                            addMarker(entity);
                        }

                        break;
                    case HolderType.TOWN:
                        entitiesCounts[1] += 1;
                        if (shouldMarkTown)
                            ecb.AddComponent(entity.Index + 10000, entity, new Marked());
                        break;
                    case HolderType.GOLD_MINE:
                    case HolderType.MILL:
                    case HolderType.STONE_MINE:
                    case HolderType.LUMBERJACK_HUT:
                        entitiesCounts[2] += 1;
                        if (shouldMarkMinor)
                            ecb.AddComponent(entity.Index + 10000, entity, new Marked());
                        break;
                    case HolderType.TOWN_DEPLOYER:
                        if (shouldMarkTown)
                            ecb.AddComponent(entity.Index + 10000, entity, new Marked());
                        break;
                    default: throw new Exception("Unknown type");
                }
            }
        }

        private void addMarker(Entity armyEntity)
        {
            var newEntity = ecb.Instantiate(armyEntity.Index + 20000, markerprefab);
            ecb.SetName(armyEntity.Index + 40000, newEntity, "Marker");
            ecb.AddComponent(armyEntity.Index + 30000, newEntity, new Parent
            {
                Value = armyEntity
            });
        }
    }
}