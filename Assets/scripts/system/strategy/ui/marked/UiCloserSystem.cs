using System;
using _Monobehaviors.minor_ui;
using _Monobehaviors.resource;
using _Monobehaviors.town_buildings_ui;
using _Monobehaviors.ui;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.selection;
using Unity.Burst;
using Unity.Entities;

namespace system.strategy.ui
{
    public partial struct UiCloserSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<InterfaceState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var interfaceState = SystemAPI.GetSingletonRW<InterfaceState>();

            if (interfaceState.ValueRO.state == interfaceState.ValueRO.oldState)
            {
                return;
            }

            if (interfaceState.ValueRO.state == UIState.GET_NEW_STATE)
            {
                return;
            }

            if (interfaceState.ValueRO.state == UIState.ALL_CLOSED)
            {
                removeOldMarks(state, interfaceState.ValueRO);
            }

            switch (interfaceState.ValueRO.oldState)
            {
                case UIState.ARMY_UI:
                    CompaniesPanel.instance.changeActive(false);
                    ArmyResource.instance.changeActive(false);
                    break;
                case UIState.TOWN_UI:
                    TownUi.instance.changeActive(false);
                    break;
                case UIState.MINOR_UI:
                    MinorUi.instance.changeActive(false);
                    break;
                case UIState.CARAVAN_UI:
                    CaravanUi.instance.changeActive(false);
                    break;
                case UIState.TOWN_BUILDINGS_UI:
                    TownBuildingsUi.instance.changeActive(false);
                    break;
                case UIState.ALL_CLOSED:
                case UIState.GET_NEW_STATE:
                    break;
                default:
                    throw new Exception("unknown state");
            }

            switch (interfaceState.ValueRO.state)
            {
                case UIState.ARMY_UI:
                    CompaniesPanel.instance.changeActive(true);
                    break;
                case UIState.TOWN_UI:
                    TownUi.instance.changeActive(true);
                    break;
                case UIState.MINOR_UI:
                    MinorUi.instance.changeActive(true);
                    break;
                case UIState.CARAVAN_UI:
                    CaravanUi.instance.changeActive(true);
                    break;
                case UIState.TOWN_BUILDINGS_UI:
                    TownBuildingsUi.instance.changeActive(true);
                    break;
                case UIState.ALL_CLOSED:
                case UIState.GET_NEW_STATE:
                    break;
                default:
                    throw new Exception("unknown state");
            }

            interfaceState.ValueRW.oldState = interfaceState.ValueRW.state;
        }

        private void removeOldMarks(SystemState state, InterfaceState interfaceState)
        {
            if (interfaceState.state != UIState.ALL_CLOSED)
            {
                return;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new RemoveOldMarkersJob
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
            new RemoveOldMarkersVisualJob
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct RemoveOldMarkersJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Marked marked, Entity entity)
            {
                ecb.RemoveComponent<Marked>(entity.Index, entity);
            }
        }

        [BurstCompile]
        public partial struct RemoveOldMarkersVisualJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(ArmyMarkerTag tag, Entity entity)
            {
                ecb.DestroyEntity(entity.Index, entity);
            }
        }
    }
}