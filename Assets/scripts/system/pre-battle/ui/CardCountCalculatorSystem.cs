using System.Collections.Generic;
using _Monobehaviors.ui_toolkit.pre_battle;
using component;
using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.cards;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.pre_battle
{
    public partial struct CardCountCalculatorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cardInfos = SystemAPI.GetSingletonBuffer<CardInfo>();
            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var uiState = SystemAPI.GetSingleton<PreBattleUiState>();

            var battalionCounter = new NativeHashMap<(Team, SoldierType), int>(cardInfos.Length, Allocator.TempJob);
            foreach (var battalionToSpawn in battalionsToSpawn)
            {
                if (battalionToSpawn.position.HasValue)
                {
                    continue;
                }

                var key = (battalionToSpawn.team, battalionToSpawn.armyType);
                if (battalionCounter.TryGetValue(key, out var count))
                {
                    battalionCounter[key] = count + 1;
                }
                else
                {
                    battalionCounter.Add(key, 1);
                }
            }

            var cardChanged = false;
            for (int i = 0; i < cardInfos.Length; i++)
            {
                var cardInfo = cardInfos[i];
                var key = (cardInfo.team, cardInfo.soldierType);
                if (battalionCounter.TryGetValue(key, out var count))
                {
                    if (count == cardInfo.battalionCount)
                    {
                        continue;
                    }

                    var newValue = cardInfo;
                    newValue.battalionCount = count;
                    cardInfos[i] = newValue;

                    cardChanged = true;
                }
            }

            if (cardChanged || uiState.preBattleEvent == PreBattleEvent.INIT)
            {
                var sortedCards = getSortedCards(cardInfos);
                CardsUi.instance.updateCardLabels(sortedCards);
                sortedCards.Dispose();
            }

            battalionCounter.Dispose();
        }

        private NativeArray<CardInfo> getSortedCards(DynamicBuffer<CardInfo> cards)
        {
            var result = cards.ToNativeArray(Allocator.TempJob);
            result.Sort(new CardInfoComparator());
            return result;
        }

        public class CardInfoComparator : IComparer<CardInfo>
        {
            public int Compare(CardInfo e1, CardInfo e2)
            {
                if (e1.team != e2.team)
                {
                    return e1.team == Team.TEAM1 ? -1 : 1;
                }

                return e1.soldierType.CompareTo(e2.soldierType);
            }
        }
    }
}