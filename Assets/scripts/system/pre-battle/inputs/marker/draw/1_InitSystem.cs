using component._common.system_switchers;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Entities;

namespace system.pre_battle.inputs
{
    public partial struct InitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var preBattlePositionMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            if (preBattlePositionMarker.ValueRO.state != PreBattleMarkerState.INIT)
            {
                return;
            }

            preBattlePositionMarker.ValueRW.state = PreBattleMarkerState.RUNNING;

            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (!card.teamTmp.HasValue && !card.soldierTypeTmp.HasValue)
                {
                    continue;
                }

                cards[i] = new PreBattleBattalion
                {
                    position = card.position,
                    entity = card.entity,
                    soldierType = card.soldierTypeTmp,
                    team = card.teamTmp,
                    teamTmp = card.team,
                    soldierTypeTmp = card.soldierType
                };
            }
        }
    }
}