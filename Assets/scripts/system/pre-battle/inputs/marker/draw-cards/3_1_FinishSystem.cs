using component._common.system_switchers;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Entities;

namespace system.pre_battle.inputs
{
    [UpdateAfter(typeof(DrawNewlyMarkedSystem))]
    public partial struct FinishSystem : ISystem
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
            if (preBattlePositionMarker.ValueRO.state != PreBattleMarkerState.FINISHED)
            {
                return;
            }

            preBattlePositionMarker.ValueRW.state = PreBattleMarkerState.IDLE;

            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];

                cards[i] = new PreBattleBattalion
                {
                    position = card.position,
                    entity = card.entity,
                    soldierType = card.soldierTypeTmp,
                    team = card.teamTmp,
                    battalionId = card.battalionIdTmp,
                    teamTmp = null,
                    soldierTypeTmp = null,
                    battalionIdTmp = null,
                    marked = false
                };
            }
        }
    }
}