using System.Collections.Generic;
using _Monobehaviors.ui.battle_plan.buttons;
using _Monobehaviors.ui.battle_plan.counter;
using Unity.Mathematics;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.battle_grid
{
    public class GridSpawner : MonoBehaviour
    {
        public static GridSpawner instance;
        [SerializeField] private int gridCount;
        [SerializeField] private GameObject gridPrefab;
        [SerializeField] private GameObject target;
        private List<GameObject> oldButtons = new();

        private void Awake()
        {
            instance = this;
        }

        public void spawn()
        {
            clearOldButtons();

            for (var i = 0; i < gridCount; i++)
            {
                var newInstance = Instantiate(gridPrefab, target.transform);
                oldButtons.Add(newInstance);
                var newPosition = new int2(i / 10, i % 10);
                setupPosition(newInstance, newPosition);
            }
        }

        private void clearOldButtons()
        {
            oldButtons.ForEach(old => { Destroy(old); });
        }

        private void setupPosition(GameObject newInstance, int2 newPosition)
        {
            var dragTarget = newInstance.GetComponent<ButtonDropTarget>();
            dragTarget.position = newPosition;
            ArmyFormationManager.instance.add(dragTarget);
        }
    }
}