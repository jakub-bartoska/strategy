using component;
using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace system.pre_battle.inputs
{
    [UpdateAfter(typeof(InitSystem))]
    public partial struct RunningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var preBattlePositionMarker = SystemAPI.GetSingleton<PreBattlePositionMarker>();
            if (preBattlePositionMarker.state != PreBattleMarkerState.RUNNING)
            {
                return;
            }

            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
            var positions = createPositions(preBattlePositionMarker);

            var preBattleUiState = SystemAPI.GetSingleton<PreBattleUiState>();
            if (preBattleUiState.selectedCard == null)
            {
                return;
            }

            var removeCall = preBattlePositionMarker.MarkerType == MarkerType.REMOVE;

            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                bool? marked = null;

                if (!isPositionSelected(positions, card))
                {
                    marked = false;
                }

                //mark only cards which are not selected from previous times
                if (attributesMatch(card, preBattleUiState, removeCall))
                {
                    marked = false;
                }

                if (!marked.HasValue)
                {
                    marked = true;
                }

                cards[i] = new PreBattleBattalion
                {
                    position = card.position,
                    entity = card.entity,
                    soldierType = card.soldierType,
                    team = card.team,
                    battalionId = card.battalionId,
                    teamTmp = card.teamTmp,
                    soldierTypeTmp = card.soldierTypeTmp,
                    battalionIdTmp = card.battalionIdTmp,
                    marked = marked.Value
                };
            }
        }

        private bool attributesMatch(PreBattleBattalion card, PreBattleUiState uiState, bool removeCall)
        {
            Team? team = !removeCall ? uiState.selectedTeam : null;
            SoldierType? soldierType = !removeCall ? uiState.selectedCard : null;

            if (card.team != team)
            {
                return false;
            }

            return card.soldierType == soldierType;
        }

        private bool isPositionSelected(MarkerPositions marked, PreBattleBattalion card)
        {
            if (card.position.x < marked.minX || card.position.x > marked.maxX)
            {
                return false;
            }

            return card.position.z >= marked.minZ && card.position.z <= marked.maxZ;
        }

        private MarkerPositions createPositions(PreBattlePositionMarker preBattlePositionMarker)
        {
            return new MarkerPositions
            {
                minX = math.min(preBattlePositionMarker.startPosition.Value.x, preBattlePositionMarker.endPosition.Value.x),
                maxX = math.max(preBattlePositionMarker.startPosition.Value.x, preBattlePositionMarker.endPosition.Value.x),
                minZ = math.min(preBattlePositionMarker.startPosition.Value.y, preBattlePositionMarker.endPosition.Value.y),
                maxZ = math.max(preBattlePositionMarker.startPosition.Value.y, preBattlePositionMarker.endPosition.Value.y)
            };
        }

        public struct MarkerPositions
        {
            public float minX;
            public float maxX;
            public float minZ;
            public float maxZ;
        }
    }
}