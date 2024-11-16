using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.pre_battle.inputs
{
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct OrderMarkedSystem : ISystem
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

            //for remove call, all fields are always marked, no need to sort them
            if (preBattlePositionMarker.MarkerType == MarkerType.REMOVE)
            {
                return;
            }

            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();

            var preBattleUiState = SystemAPI.GetSingleton<PreBattleUiState>();
            if (preBattleUiState.selectedCard == null)
            {
                return;
            }

            var battalions = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var battalionIds = getBattalionIdsByTeamAndType(preBattleUiState.selectedTeam, preBattleUiState.selectedCard.Value, battalions);

            var markedCards = new NativeList<PreBattleBattalion>(Allocator.Temp);
            foreach (var card in cards)
            {
                if (card.marked)
                {
                    markedCards.Add(card);
                }
            }

            if (battalionIds.Length > markedCards.Length)
            {
                return;
            }

            markedCards.Sort(new CardSortByPositionDesc(preBattlePositionMarker.startPosition.Value, preBattlePositionMarker.endPosition.Value));

            var cardsToUnmark = new NativeHashSet<float3>(markedCards.Length, Allocator.Temp);
            for (int i = battalionIds.Length; i < markedCards.Length; i++)
            {
                cardsToUnmark.Add(markedCards[i].position);
            }

            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (cardsToUnmark.Contains(card.position))
                {
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
                        marked = false
                    };
                }
            }
        }

        public class CardSortByPositionDesc : IComparer<PreBattleBattalion>
        {
            private bool downToUp;
            private bool leftToRight;

            public CardSortByPositionDesc(float2 startPosition, float2 endPosition)
            {
                this.leftToRight = startPosition.x < endPosition.x;
                this.downToUp = startPosition.y < endPosition.y;
            }

            public int Compare(PreBattleBattalion card1, PreBattleBattalion card2)
            {
                if (card1.position.x != card2.position.x)
                {
                    if (leftToRight)
                    {
                        return card1.position.x.CompareTo(card2.position.x);
                    }

                    return card2.position.x.CompareTo(card1.position.x);
                }

                if (downToUp)
                {
                    return card1.position.z.CompareTo(card2.position.z);
                }

                return card2.position.z.CompareTo(card1.position.z);
            }
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
    }
}