using component.config.game_settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class ButtonDropTarget : MonoBehaviour, IDropHandler
    {
        public int2 position;
        private BattalionToSpawn? batalion;

        public void OnDrop(PointerEventData eventData)
        {
            if (batalion.HasValue) return;

            var draggableButton = eventData.pointerDrag.GetComponent<DraggableButton>();
            draggableButton.setNewParent(transform);
            batalion = draggableButton.getBatalion();
        }

        public void add()
        {
            if (batalion.HasValue) return;

            //fetch batalion
        }

        public void remove()
        {
            if (!batalion.HasValue) return;

            //return value
        }

        public void emptyBatalion()
        {
            batalion = null;
        }

        public BattalionToSpawn? getBatalion()
        {
            if (batalion.HasValue)
            {
                var tmp = batalion.Value;
                tmp.position = position;
                batalion = tmp;
            }

            return batalion;
        }
    }
}