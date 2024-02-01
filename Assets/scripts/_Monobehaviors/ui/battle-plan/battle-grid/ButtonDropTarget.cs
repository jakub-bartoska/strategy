using _Monobehaviors.ui.battle_plan.counter;
using component;
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
            battalion = draggableButton.getBattalion();
        }

        public void add(Team team)
        {
            if (battalion.HasValue) return;

            battalion = ArmyFormationManager.instance.tryToGetBattalion(team);

            if (!battalion.HasValue) return;

            var newInstance = Instantiate(armyPrefab, transform);
            draggableButton = newInstance.GetComponent<DraggableButton>();
            draggableButton.setNewParent(transform);
            draggableButton.setBattalion(battalion.Value);
        }

        public void remove()
        {
            if (!battalion.HasValue) return;

            ArmyFormationManager.instance.returnBattalion(battalion.Value);
            battalion = null;
            Destroy(draggableButton.gameObject);
            draggableButton = null;
        }

        public void emptyBattalion()
        {
            battalion = null;
            draggableButton = null;
        }

        public BattalionToSpawn? getBattalion()
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