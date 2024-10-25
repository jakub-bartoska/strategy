using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.pre_battle;
using component.pre_battle.marker;
using system.battle.utils.pre_battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.pre_battle.inputs
{
    public partial struct BattalionRedrawSystem : ISystem
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
            if (preBattlePositionMarker.state == PreBattleMarkerState.IDLE)
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (attributesMatch(card, preBattleUiState))
                {
                    continue;
                }

                if (!isPositionSelected(positions, card))
                {
                    continue;
                }

                state.EntityManager.DestroyEntity(card.entity);

                var newEntity = TileSpawner.spawnTile(card.position, prefabHolder, ecb, preBattleUiState.selectedTeam, preBattleUiState.selectedCard);

                cards[i] = new PreBattleBattalion
                {
                    position = card.position,
                    //entity = newEntity,
                    soldierType = cards[i].soldierType,
                    team = cards[i].team,
                    teamTmp = preBattleUiState.selectedTeam,
                    soldierTypeTmp = preBattleUiState.selectedCard
                };
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            //todo implement finished
        }

        private bool attributesMatch(PreBattleBattalion card, PreBattleUiState uiState)
        {
            if (card.teamTmp != uiState.selectedTeam)
            {
                return false;
            }

            return card.soldierTypeTmp == uiState.selectedCard;
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