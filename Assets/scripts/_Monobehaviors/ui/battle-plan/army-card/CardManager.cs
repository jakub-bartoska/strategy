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
        private Dictionary<SoldierType, ArmyCard> cards = new();

        private void Awake()
        {
            instance = this;
        }

        public void spawn(Dictionary<SoldierType, List<BattalionToSpawn>> batalions)
        {
            foreach (var (type, list) in batalions)
            {
                var newInstance = Instantiate(cardPrefab, target.transform);
                var card = newInstance.GetComponent<ArmyCard>();
                card.setTypeText(type);
                card.setMax(list.Count);
                card.setCountText(list.Count);
                cards.Add(type, card);
            }
        }

        public void updateCard(SoldierType type, int count)
        {
            cards[type].setCountText(count);
        }
    }
}