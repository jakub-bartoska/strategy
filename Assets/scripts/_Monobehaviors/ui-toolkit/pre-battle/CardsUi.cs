using System;
using System.Collections.Generic;
using component;
using component.config.game_settings;
using component.pre_battle;
using component.pre_battle.cards;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class CardsUi : MonoBehaviour
    {
        public static CardsUi instance;

        private Dictionary<CardKey, VisualElement> cards = new();
        private EntityManager entityManager;
        private VisualElement root;
        private EntityQuery uiStateQuery;

        private void Awake()
        {
            instance = this;
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            uiStateQuery = entityManager.CreateEntityQuery(typeof(PreBattleUiState));

            cards.Add(new CardKey
            {
                team = Team.TEAM1,
                soldierType = SoldierType.ARCHER
            }, root.Q<VisualElement>("soldier-type-archer-team1"));
            cards.Add(new CardKey
            {
                team = Team.TEAM1,
                soldierType = SoldierType.SWORDSMAN
            }, root.Q<VisualElement>("soldier-type-swordsman-team1"));
            cards.Add(new CardKey
            {
                team = Team.TEAM1,
                soldierType = SoldierType.CAVALRY
            }, root.Q<VisualElement>("soldier-type-cavalry-team1"));
            cards.Add(new CardKey
            {
                team = Team.TEAM2,
                soldierType = SoldierType.ARCHER
            }, root.Q<VisualElement>("soldier-type-archer-team2"));
            cards.Add(new CardKey
            {
                team = Team.TEAM2,
                soldierType = SoldierType.SWORDSMAN
            }, root.Q<VisualElement>("soldier-type-swordsman-team2"));
            cards.Add(new CardKey
            {
                team = Team.TEAM2,
                soldierType = SoldierType.CAVALRY
            }, root.Q<VisualElement>("soldier-type-cavalry-team2"));

            foreach (var (key, value) in cards)
            {
                value.RegisterCallback<ClickEvent>(_ => cardSelectedEvent(key.soldierType));
            }
        }

        public void updateCardLabels(NativeArray<CardInfo> cards)
        {
            foreach (var card in cards)
            {
                updateCardLabel(card);
            }
        }

        public void updateSelected(PreBattleUiState state)
        {
            foreach (var (key, card) in cards)
            {
                var selected = state.selectedCard == key.soldierType && state.selectedTeam == key.team;
                setMarked(selected, card);
                card.style.display = getDisplayStyle(state.selectedTeam == key.team);
            }
        }

        private void updateCardLabel(CardInfo cardInfo)
        {
            var cardKey = new CardKey
            {
                team = cardInfo.team,
                soldierType = cardInfo.soldierType
            };
            if (cards.TryGetValue(cardKey, out var card))
            {
                card.Q<Label>().text = cardInfo.currentBattalionCount + "/" + cardInfo.maxBattalionCount;
            }
        }

        private void setMarked(bool marked, VisualElement card)
        {
            if (marked)
            {
                card.AddToClassList("card-selected");
            }
            else
            {
                card.RemoveFromClassList("card-selected");
            }
        }

        private DisplayStyle getDisplayStyle(bool enabled)
        {
            if (enabled)
            {
                return DisplayStyle.Flex;
            }

            return DisplayStyle.None;
        }

        private void cardSelectedEvent(SoldierType soldierType)
        {
            var uiState = uiStateQuery.GetSingletonRW<PreBattleUiState>();
            uiState.ValueRW.selectedCard = soldierType;
            uiState.ValueRW.preBattleEvent = PreBattleEvent.CARD_CHANGED;
        }
    }

    class CardKey : IEquatable<CardKey>
    {
        public SoldierType soldierType;
        public Team team;

        public bool Equals(CardKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return team == other.team && soldierType == other.soldierType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) team, (int) soldierType);
        }
    }
}