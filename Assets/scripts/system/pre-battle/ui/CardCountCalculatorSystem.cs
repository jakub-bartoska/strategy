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

            var battalionMaxCounter = new NativeHashMap<(Team, SoldierType), int>(cardInfos.Length, Allocator.TempJob);
            var battalionCurrentCounter = new NativeHashMap<(Team, SoldierType), int>(cardInfos.Length, Allocator.TempJob);
            foreach (var battalionToSpawn in battalionsToSpawn)
            {
                var key = (battalionToSpawn.team, battalionToSpawn.armyType);
                if (battalionMaxCounter.TryGetValue(key, out var count))
                {
                    battalionMaxCounter[key] = count + 1;
                }
                else
                {
                    battalionMaxCounter.Add(key, 1);
                }

                if (battalionToSpawn.isUsed) continue;

                if (battalionCurrentCounter.TryGetValue(key, out var currentCount))
                {
                    battalionCurrentCounter[key] = currentCount + 1;
                }
                else
                {
                    battalionCurrentCounter.Add(key, 1);
                }
            }

            var cardChanged = false;
            for (int i = 0; i < cardInfos.Length; i++)
            {
                var cardInfo = cardInfos[i];
                var key = (cardInfo.team, cardInfo.soldierType);
                var maxCount = battalionMaxCounter.ContainsKey(key) ? battalionMaxCounter[key] : 0;
                var currentCount = battalionCurrentCounter.ContainsKey(key) ? battalionCurrentCounter[key] : 0;

                if (maxCount == cardInfo.maxBattalionCount && currentCount == cardInfo.currentBattalionCount)
                {
                    continue;
                }

                var newValue = cardInfo;
                newValue.maxBattalionCount = maxCount;
                newValue.currentBattalionCount = currentCount;
                cardInfos[i] = newValue;

                cardChanged = true;
            }

            if (cardChanged || uiState.preBattleEvent == PreBattleEvent.INIT)
            {
                var sortedCards = getSortedCards(cardInfos);
                CardsUi.instance.updateCardLabels(sortedCards);
                sortedCards.Dispose();
            }

            battalionMaxCounter.Dispose();
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