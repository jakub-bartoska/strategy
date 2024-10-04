using System;
using component;
using component.config.game_settings;
using component.pre_battle.cards;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class PreBattleUi : MonoBehaviour
    {
        public static PreBattleUi instance;
        private EntityQuery cardsQuery;

        private EntityManager entityManager;
        private VisualElement root;
        private VisualElement team1ArcherCard;
        private VisualElement team1CavalryCard;

        private VisualElement team1SwordsmanCard;
        private VisualElement team2ArcherCard;
        private VisualElement team2CavalryCard;

        private VisualElement team2SwordsmanCard;

        private void Awake()
        {
            instance = this;
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            cardsQuery = entityManager.CreateEntityQuery(typeof(CardInfo));

            team1SwordsmanCard = root.Q<VisualElement>("soldier-type-swordsman-team1");
            team1ArcherCard = root.Q<VisualElement>("soldier-type-cavalry-team1");
            team1CavalryCard = root.Q<VisualElement>("soldier-type-archer-team1");
            team2SwordsmanCard = root.Q<VisualElement>("soldier-type-swordsman-team2");
            team2ArcherCard = root.Q<VisualElement>("soldier-type-cavalry-team2");
            team2CavalryCard = root.Q<VisualElement>("soldier-type-archer-team2");

            var buffer = cardsQuery.GetSingletonBuffer<CardInfo>();
            updateCards(buffer);
        }

        public void addSoldierCards()
        {
            team1SwordsmanCard = root.Q<VisualElement>("soldier-type");
            var footer = root.Q<VisualElement>("footer");
            //soldierTypeCard.visualTreeAssetSource.CloneTree();
            //var newOne = new VisualElement(this.soldierTypeCard);
            //soldierTypeCard.
            //var button = new Button(() => Debug.Log("Button clicked"));
            //button.text = "Click me!";
            //rootVisualElement.Add(button);
        }

        public void updateCards(DynamicBuffer<CardInfo> cards)
        {
            foreach (var card in cards)
            {
                Debug.Log("card: " + card.soldierType + " count: " + card.battalionCount);
                updateCard(card);
            }
        }

        private void updateCard(CardInfo cardInfo)
        {
            if (cardInfo.team == Team.TEAM1)
            {
                switch (cardInfo.soldierType)
                {
                    case SoldierType.SWORDSMAN:
                        if (team1SwordsmanCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team1SwordsmanCard.SetEnabled(cardInfo.enabled);
                        }

                        team1SwordsmanCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    case SoldierType.ARCHER:
                        if (team1ArcherCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team1ArcherCard.SetEnabled(cardInfo.enabled);
                        }

                        team1ArcherCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    case SoldierType.CAVALRY:
                        if (team1CavalryCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team1CavalryCard.SetEnabled(cardInfo.enabled);
                        }

                        team1CavalryCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (cardInfo.soldierType)
                {
                    case SoldierType.SWORDSMAN:
                        if (team2SwordsmanCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team2SwordsmanCard.SetEnabled(cardInfo.enabled);
                        }

                        team2SwordsmanCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    case SoldierType.ARCHER:
                        if (team2ArcherCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team2ArcherCard.SetEnabled(cardInfo.enabled);
                        }

                        team2ArcherCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    case SoldierType.CAVALRY:
                        if (team2CavalryCard.enabledInHierarchy != cardInfo.enabled)
                        {
                            team2CavalryCard
                            team2CavalryCard.SetEnabled(cardInfo.enabled);
                        }

                        team2CavalryCard.Q<Label>().text = cardInfo.battalionCount.ToString();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}