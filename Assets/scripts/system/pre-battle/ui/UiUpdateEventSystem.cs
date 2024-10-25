using _Monobehaviors.ui_toolkit.pre_battle;
using component._common.system_switchers;
using component.pre_battle;
using Unity.Burst;
using Unity.Entities;

namespace system.pre_battle
{
    public partial struct UiUpdateEventSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var preBattleUiState = SystemAPI.GetSingletonRW<PreBattleUiState>();
            if (preBattleUiState.ValueRO.preBattleEvent != PreBattleEvent.NONE)
            {
                TeamButtons.instance.updateTeamButtons(preBattleUiState.ValueRO);
                CardsUi.instance.updateSelected(preBattleUiState.ValueRO);

                preBattleUiState.ValueRW.preBattleEvent = PreBattleEvent.NONE;
            }
        }
    }
}