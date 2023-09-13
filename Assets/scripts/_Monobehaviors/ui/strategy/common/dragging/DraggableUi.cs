using UnityEngine;

namespace _Monobehaviors.ui
{
    public interface DraggableUi
    {
        public void updateDragging(bool newDraggingstate);

        public void finishDrag(long companyId, Vector3 targetPosition);
    }
}