using System;
using component;
using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.general;
using component.strategy.army_components;
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
            var teamColors = SystemAPI.GetSingletonBuffer<TeamColor>();
            var playerSettings = SystemAPI.GetSingleton<GamePlayerSettings>();
            var interfaceState = SystemAPI.GetSingletonRW<InterfaceState>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            switch (marker.ValueRO.state)
            {
                case MarkerState.RUNNING:
                    new ArmyMarkerJob
                        {
                            MarkerState = marker.ValueRO,
                            colors = teamColors,
                            gamePlayerSettings = playerSettings
                        }.ScheduleParallel(state.Dependency)
                        .Complete();
                    return;
                case MarkerState.IDLE:
                    return;
                case MarkerState.FINISHED:
                    new ArmyMarkerJob
                        {
                            MarkerState = marker.ValueRO,
                            colors = teamColors,
                            gamePlayerSettings = playerSettings
                        }.ScheduleParallel(state.Dependency)
                        .Complete();
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
                    var markerPrefab = SystemAPI.GetSingleton<PrefabHolder>().markerPrefab;
                    new MarkEntitiesJob
                        {
                            markerState = marker.ValueRO,
                            gamePlayerSettings = playerSettings,
                            ecb = ecb.AsParallelWriter(),
                            markerprefab = markerPrefab
                        }.ScheduleParallel(state.Dependency)
                        .Complete();
                    interfaceState.ValueRW.oldState = interfaceState.ValueRW.state;
                    interfaceState.ValueRW.state = UIState.GET_NEW_STATE;
                    marker.ValueRW.state = MarkerState.IDLE;
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [BurstCompile]
    public partial struct ArmyMarkerJob : IJobEntity
    {
        [ReadOnly] public SelectionMarkerState MarkerState;
        [ReadOnly] public DynamicBuffer<TeamColor> colors;
        [ReadOnly] public GamePlayerSettings gamePlayerSettings;

        private void Execute(LocalTransform transform, ref MaterialColorComponent materialColor, ArmyTag tag,
            TeamComponent team)
        {
            if (team.team != gamePlayerSettings.playerTeam)
            {
                return;
            }

            var minX = math.min(MarkerState.min.x, MarkerState.max.x);
            var maxX = math.max(MarkerState.min.x, MarkerState.max.x);
            var minZ = math.min(MarkerState.min.z, MarkerState.max.z);
            var maxZ = math.max(MarkerState.min.z, MarkerState.max.z);

            if (transform.Position.x > minX && transform.Position.x < maxX &&
                transform.Position.z > minZ && transform.Position.z < maxZ)
            {
                materialColor.Value = getColor(team.team) * 3;
            }
            else
            {
                materialColor.Value = getColor(team.team);
            }
        }

        private float4 getColor(Team team)
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
    public partial struct MarkEntitiesJob : IJobEntity
    {
        [ReadOnly] public SelectionMarkerState markerState;
        [ReadOnly] public GamePlayerSettings gamePlayerSettings;
        public EntityCommandBuffer.ParallelWriter ecb;
        public Entity markerprefab;

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
                ecb.AddComponent(entity.Index + 10000, entity, new Marked());

                if (idHolder.type != HolderType.ARMY)
                {
                    return;
                }

                addMarker(entity);
            }
        }

        private void addMarker(Entity amyEntity)
        {
            var newEntity = ecb.Instantiate(amyEntity.Index + 20000, markerprefab);
            ecb.SetName(amyEntity.Index + 40000, newEntity, "Marker");
            ecb.AddComponent(amyEntity.Index + 30000, newEntity, new Parent
            {
                Value = amyEntity
            });
        }
    }
}