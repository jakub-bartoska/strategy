using UnityEngine;
using UnityEngine.EventSystems;

namespace _Monobehaviors.ui
{
    public class CompaniesDragging : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private long myCompanyId;
        private DraggableUi ui;

        public void OnBeginDrag(PointerEventData eventData)
        {
            ui.updateDragging(true);
            transform.SetAsLastSibling();
            drag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            drag();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ui.updateDragging(false);
            ui.finishDrag(myCompanyId, transform.position);
        }

        public void setUi(DraggableUi ui)
        {
            this.ui = ui;
        }

        public void setCompanyId(long companyId)
        {
            myCompanyId = companyId;
        }

        private void drag()
        {
            var currentPosition = Input.mousePosition;
            currentPosition.x -= 35;
            currentPosition.y -= 45;
            transform.position = currentPosition;
        }
    }
}