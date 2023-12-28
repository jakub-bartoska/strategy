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
            Debug.Log("clicked by: " + eventData.button);
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    buttonDropTarget.a
                    break;
                case PointerEventData.InputButton.Right:
                    Debug.Log("right click");
                    break;
                case PointerEventData.InputButton.Middle:
                    Debug.Log("middle click");
                    break;
            }
        }
    }
}