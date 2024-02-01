using _Monobehaviors.ui.battle_plan.buttons;
using component;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Monobehaviors.ui.battle_plan.battle_grid
{
    public class ButtonClickable : MonoBehaviour, IPointerClickHandler
    {
        private ButtonDropTarget buttonDropTarget;

        private void Awake()
        {
            buttonDropTarget = GetComponent<ButtonDropTarget>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    buttonDropTarget.add(Team.TEAM1);
                    break;
                case PointerEventData.InputButton.Right:
                    buttonDropTarget.add(Team.TEAM2);
                    //buttonDropTarget.remove();
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }
    }
}