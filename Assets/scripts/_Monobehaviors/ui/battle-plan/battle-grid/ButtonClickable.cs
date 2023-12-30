using _Monobehaviors.ui.battle_plan.buttons;
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
                    buttonDropTarget.add();
                    break;
                case PointerEventData.InputButton.Right:
                    buttonDropTarget.remove();
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }
    }
}