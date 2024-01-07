using _Monobehaviors.ui.manager;
using component.config.game_settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class DraggableButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BattalionToSpawn battalion;
        private Image image;
        private Transform parentAfterDrag;
        private Transform parentBeforeDrag;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            parentBeforeDrag = transform.parent;
            parentAfterDrag = transform.parent;
            transform.SetParent(UiManager.instance.getBattlePlanUiTransform());
            transform.SetAsLastSibling();
            image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.SetParent(parentAfterDrag);
            image.raycastTarget = true;

            if (parentAfterDrag == parentBeforeDrag) return;

            var buttonDropTarget = parentBeforeDrag.GetComponent<ButtonDropTarget>();
            buttonDropTarget.emptyBattalion();
        }

        public void setNewParent(Transform newParent)
        {
            parentAfterDrag = newParent;
        }

        public void setBattalion(BattalionToSpawn battalion)
        {
            this.battalion = battalion;
        }

        public BattalionToSpawn getBattalion()
        {
            return battalion;
        }
    }
}