using _Monobehaviors.ui.battle_plan.counter;
using component.config.game_settings;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class ButtonDropTarget : MonoBehaviour, IDropHandler
    {
        public int2 position;
        [SerializeField] private GameObject armyPrefab;
        private BattalionToSpawn? battalion;
        [CanBeNull] private DraggableButton draggableButton;

        public void OnDrop(PointerEventData eventData)
        {
            if (battalion.HasValue) return;

            draggableButton = eventData.pointerDrag.GetComponent<DraggableButton>();
            draggableButton.setNewParent(transform);
            battalion = draggableButton.getBatalion();
        }

        public void add()
        {
            if (battalion.HasValue) return;

            battalion = ArmyFormationManager.instance.tryToGetBattalion();

            if (!battalion.HasValue) return;

            var newInstance = Instantiate(armyPrefab, transform);
            draggableButton = newInstance.GetComponent<DraggableButton>();
            draggableButton.setNewParent(transform);
            draggableButton.setBatalion(battalion.Value);
        }

        public void remove()
        {
            if (!battalion.HasValue) return;

            ArmyFormationManager.instance.returnBatalion(battalion.Value);
            battalion = null;
            Destroy(draggableButton.gameObject);
            draggableButton = null;
        }

        public void emptyBatalion()
        {
            battalion = null;
            draggableButton = null;
        }

        public BattalionToSpawn? getBatalion()
        {
            if (battalion.HasValue)
            {
                var tmp = battalion.Value;
                tmp.position = position;
                battalion = tmp;
            }

            return battalion;
        }
    }
}