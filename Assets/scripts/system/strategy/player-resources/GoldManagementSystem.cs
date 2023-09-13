using _Monobehaviors.ui.player_resources;
using component._common.system_switchers;
using component.strategy.player_resources;
using Unity.Burst;
using Unity.Entities;

namespace system.strategy.player_resources
{
    public partial struct GoldManagementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<GoldHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var goldHolder = SystemAPI.GetSingletonRW<GoldHolder>();

            goldHolder.ValueRW.timeRemaining -= deltaTime;

            if (goldHolder.ValueRW.timeRemaining > 0) return;

            goldHolder.ValueRW.timeRemaining += 1;
            goldHolder.ValueRW.gold += goldHolder.ValueRW.goldPerSecond;
            GoldUi.instance.updateGold(goldHolder.ValueRW.gold);
        }
    }
}