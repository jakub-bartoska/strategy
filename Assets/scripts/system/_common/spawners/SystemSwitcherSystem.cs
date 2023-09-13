using _Monobehaviors.ui;
using component._common.general;
using component._common.system_switchers;
using Unity.Burst;
using Unity.Entities;

namespace system._common
{
    public partial struct SystemSwitcherSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SingletonEntityTag>();
            state.RequireForUpdate<SystemStatusHolder>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();
            if (blockers.Length != 0) return;

            var systemSwitch = SystemAPI.GetSingletonRW<SystemStatusHolder>();

            if (systemSwitch.ValueRO.currentStatus == systemSwitch.ValueRO.desiredStatus) return;

            var singletonEntity = SystemAPI.GetSingletonEntity<SingletonEntityTag>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            StateManagerForMonos.getInstance()
                .updateStatus(systemSwitch.ValueRO.desiredStatus, systemSwitch.ValueRO.currentStatus);
            addAndRemoveMarkers(state, systemSwitch.ValueRO.desiredStatus, singletonEntity, ecb);

            if (systemSwitch.ValueRO.desiredStatus == SystemStatus.INGAME_MENU)
            {
                systemSwitch.ValueRW.previousStatus = systemSwitch.ValueRO.currentStatus;
            }

            systemSwitch.ValueRW.currentStatus = systemSwitch.ValueRO.desiredStatus;
        }

        private void addAndRemoveMarkers(SystemState state, SystemStatus desiredStatus, Entity singletonEntity,
            EntityCommandBuffer ecb)
        {
            if (desiredStatus == SystemStatus.BATTLE)
            {
                if (!state.EntityManager.HasComponent<BattleMapStateMarker>(singletonEntity))
                {
                    var battleStarter = new BattleMapStateMarker();
                    ecb.AddComponent(singletonEntity, battleStarter);
                }
            }
            else
            {
                if (state.EntityManager.HasComponent<BattleMapStateMarker>(singletonEntity))
                {
                    ecb.RemoveComponent<BattleMapStateMarker>(singletonEntity);
                }
            }

            if (desiredStatus == SystemStatus.STRATEGY)
            {
                if (!state.EntityManager.HasComponent<StrategyMapStateMarker>(singletonEntity))
                {
                    var strategyStarter = new StrategyMapStateMarker();
                    ecb.AddComponent(singletonEntity, strategyStarter);
                }
            }
            else
            {
                if (state.EntityManager.HasComponent<StrategyMapStateMarker>(singletonEntity))
                {
                    ecb.RemoveComponent<StrategyMapStateMarker>(singletonEntity);
                }
            }
        }
    }
}