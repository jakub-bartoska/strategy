using System.Collections.Generic;
using component.config.game_settings;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.army_card
{
    public class CardManager : MonoBehaviour
    {
        public static CardManager instance;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject target;
        private Dictionary<SoldierType, (ArmyCard, ArmyCardClickable)> cards = new();

        private void Awake()
        {
            instance = this;
        }

        public void spawn(Dictionary<SoldierType, List<BattalionToSpawn>> battalions)
        {
            foreach (var (type, list) in battalions)
            {
                var newInstance = Instantiate(cardPrefab, target.transform);
                var card = newInstance.GetComponent<ArmyCard>();
                card.setTypeText(type);
                card.setMax(list.Count);
                card.setCountText(list.Count);
                var clickable = newInstance.GetComponent<ArmyCardClickable>();
                cards.Add(type, (card, clickable));
            }
        }

        public void clear()
        {
            foreach (var cardsValue in cards.Values)
            {
                Destroy(cardsValue.Item1.gameObject);
                Destroy(cardsValue.Item2.gameObject);
            }

            cards.Clear();
        }

        public void updateCard(SoldierType type, int count)
        {
            cards[type].Item1.setCountText(count);
        }

        public void updateCardColors(SoldierType newType)
        {
            foreach (var (type, (card, clickable)) in cards)
            {
                if (type == newType)
                {
                    clickable.updateActivity(CardState.ACTIVE);
                }
                else
                {
                    clickable.updateActivity(CardState.INACTIVE);
                }
            }
        }
    }
}