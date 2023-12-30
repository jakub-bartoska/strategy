using _Monobehaviors.ui.battle_plan.counter;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Monobehaviors.ui.battle_plan.army_card
{
    public class ArmyCardClickable : MonoBehaviour, IPointerClickHandler
    {
        private ArmyCard card;
        private Color green = new(0.392f, 0.588f, 0.392f, 255);
        private Image image;
        private Color red = new(0.588f, 0.392f, 0.392f, 255);
        private CardState state = CardState.INACTIVE;

        private void Awake()
        {
            card = gameObject.GetComponent<ArmyCard>();
            image = GetComponent<Image>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ArmyFormationManager.instance.updateSelectedType(card.getSoldierType());
        }

        public void updateActivity(CardState newState)
        {
            if (state == newState) return;

            state = newState;

            switch (state)
            {
                case CardState.ACTIVE:
                    image.color = green;
                    break;
                case CardState.INACTIVE:
                    image.color = red;
                    break;
            }
        }
    }
}