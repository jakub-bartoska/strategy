using component;
using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.marker;
using system.battle.utils.pre_battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.pre_battle.inputs
{
    [UpdateAfter(typeof(OrderMarkedSystem))]
    public partial struct DrawNewlyMarkedSystem : ISystem
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

            var preBattleUiState = SystemAPI.GetSingleton<PreBattleUiState>();
            if (preBattleUiState.selectedCard == null)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var entitiesToDelete = new NativeList<Entity>(Allocator.Temp);

            var battalions = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var battalionIds = getBattalionIdsByTeamAndType(preBattleUiState.selectedTeam, preBattleUiState.selectedCard.Value, battalions);

            var removeCall = preBattlePositionMarker.MarkerType == MarkerType.REMOVE;

            var newBuffer = new NativeList<PreBattleBattalion>(Allocator.Temp);
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];

                //field is not marked, but card is marked => need to redraw to old value
                if (attributesMatch(card, preBattleUiState, removeCall) && !card.marked)
                {
                    entitiesToDelete.Add(card.entity);
                    var newValue = fallBackToOldCard(card, prefabHolder, ecb);

                    newBuffer.Add(newValue);
                    continue;
                }

                if (!card.marked)
                {
                    newBuffer.Add(card);
                    continue;
                }

                //adding new battalions, but dont have any in reserves
                if (!removeCall && battalionIds.IsEmpty)
                {
                    newBuffer.Add(card);
                    continue;
                }

                var battalionId = getBattalionId(removeCall, battalionIds);

                //field is marked, but card is not marked => need to redraw to new value
                if (!attributesMatch(card, preBattleUiState, removeCall))
                {
                    entitiesToDelete.Add(card.entity);
                    var newValue = createMarkerEntity(card, prefabHolder, ecb, preBattleUiState, removeCall, battalionId);

                    newBuffer.Add(newValue);
                    continue;
                }

                //just update battalionId
                var newCard = new PreBattleBattalion
                {
                    position = card.position,
                    entity = card.entity,
                    soldierType = card.soldierType,
                    team = card.team,
                    battalionId = card.battalionId,
                    teamTmp = card.teamTmp,
                    soldierTypeTmp = card.soldierTypeTmp,
                    battalionIdTmp = battalionId,
                    marked = card.marked
                };
                newBuffer.Add(newCard);
            }

            foreach (var entity in entitiesToDelete)
            {
                ecb.DestroyEntity(entity);
            }

            var singletonEntiy = SystemAPI.GetSingletonEntity<SingletonEntityTag>();
            var createdBuffer = ecb.SetBuffer<PreBattleBattalion>(singletonEntiy);
            createdBuffer.AddRange(newBuffer);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private long? getBattalionId(bool removeCall, NativeList<long> battalionIds)
        {
            if (removeCall)
            {
                return null;
            }

            var battalionId = battalionIds[battalionIds.Length - 1];
            battalionIds.RemoveAt(battalionIds.Length - 1);

            return battalionId;
        }

        private NativeList<long> getBattalionIdsByTeamAndType(Team team, SoldierType soldierType, DynamicBuffer<BattalionToSpawn> battalions)
        {
            var result = new NativeList<long>(Allocator.Temp);
            foreach (var battalion in battalions)
            {
                if (battalion.team == team && battalion.armyType == soldierType && !battalion.isUsed)
                {
                    result.Add(battalion.battalionId);
                }
            }

            return result;
        }

        private PreBattleBattalion createMarkerEntity(PreBattleBattalion oldCard, PrefabHolder prefabHolder, EntityCommandBuffer ecb, PreBattleUiState preBattleUiState, bool removeCall,
            long? battalionId)
        {
            Team? team = !removeCall ? preBattleUiState.selectedTeam : null;
            SoldierType? soldierType = !removeCall ? preBattleUiState.selectedCard : null;
            var newEntity = TileSpawner.spawnTile(oldCard.position, prefabHolder, ecb, team, soldierType);

            return new PreBattleBattalion
            {
                position = oldCard.position,
                entity = newEntity,
                soldierType = oldCard.soldierType,
                team = oldCard.team,
                battalionId = oldCard.battalionId,
                teamTmp = team,
                soldierTypeTmp = soldierType,
                battalionIdTmp = battalionId,
                marked = oldCard.marked
            };
        }

        private PreBattleBattalion fallBackToOldCard(PreBattleBattalion oldCard, PrefabHolder prefabHolder, EntityCommandBuffer ecb)
        {
            var newEntity = TileSpawner.spawnTile(oldCard.position, prefabHolder, ecb, oldCard.team, oldCard.soldierType);

            return new PreBattleBattalion
            {
                position = oldCard.position,
                entity = newEntity,
                soldierType = oldCard.soldierType,
                team = oldCard.team,
                battalionId = oldCard.battalionId,
                teamTmp = oldCard.team,
                soldierTypeTmp = oldCard.soldierType,
                battalionIdTmp = oldCard.battalionId,
                marked = oldCard.marked
            };
        }

        private bool attributesMatch(PreBattleBattalion card, PreBattleUiState uiState, bool removeCall)
        {
            Team? team = !removeCall ? uiState.selectedTeam : null;
            SoldierType? soldierType = !removeCall ? uiState.selectedCard : null;

            if (card.teamTmp != team)
            {
                return false;
            }

            return card.soldierTypeTmp == soldierType;
        }
    }
}