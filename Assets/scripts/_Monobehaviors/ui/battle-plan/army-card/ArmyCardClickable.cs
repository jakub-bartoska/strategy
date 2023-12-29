using _Monobehaviors.ui.battle_plan.counter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Monobehaviors.ui.battle_plan.army_card
{
    public class ArmyCardClickable : MonoBehaviour, IPointerClickHandler
    {
        private ArmyCard card;

        private void Awake()
        {
            card = gameObject.GetComponent<ArmyCard>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ArmyFormationManager.instance.updateSelectedType(card.getSoldierType());
        }
    }
}