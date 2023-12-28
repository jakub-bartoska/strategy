using System;
using System.Collections.Generic;
using _Monobehaviors.ui.battle_plan.buttons;
using _Monobehaviors.ui.battle_plan.counter;
using component.config.game_settings;
using Unity.Mathematics;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.battle_grid
{
    public class GridSpawner : MonoBehaviour
    {
        public static GridSpawner battalionInstance;
        public static GridSpawner planInstance;
        [SerializeField] private int gridCount;
        [SerializeField] private GridSpawnerType type;
        [SerializeField] private GameObject gridPrefab;
        [SerializeField] private GameObject armyPrefab;
        [SerializeField] private GameObject target;
        private List<GameObject> oldButtons = new();

        private void Awake()
        {
            switch (type)
            {
                case GridSpawnerType.PLAN:
                    planInstance = this;
                    break;
                case GridSpawnerType.BATTALION:
                    battalionInstance = this;
                    break;
                default:
                    throw new Exception("unknown type " + type);
            }
        }

        public void spawnPlan()
        {
            spawn(new List<BattalionToSpawn>(), gridCount);
        }

        public void spawnBattalions(List<BattalionToSpawn> battalions)
        {
            spawn(battalions, battalions.Count);
        }

        private void spawn(List<BattalionToSpawn> battalions, int cellsToSpawn)
        {
            clearOldButtons();

            var remainingArmiesToSpwan = battalions.Count;
            for (var i = 0; i < cellsToSpawn; i++)
            {
                var newInstance = Instantiate(gridPrefab, target.transform);
                oldButtons.Add(newInstance);
                switch (type)
                {
                    case GridSpawnerType.PLAN:
                        var newPosition = new int2(i / 10, i % 10);
                        setupPosition(newInstance, newPosition);
                        break;
                    case GridSpawnerType.BATTALION:
                        if (remainingArmiesToSpwan != 0)
                        {
                            setupBattalion(newInstance.transform, battalions[remainingArmiesToSpwan - 1]);
                            remainingArmiesToSpwan--;
                        }

                        break;
                    default:
                        throw new Exception("unknown type " + type);
                }
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

        private void setupBattalion(Transform parent, BattalionToSpawn batalion)
        {
            var newBattalion = Instantiate(armyPrefab, parent);
            var newButton = newBattalion.GetComponent<DraggableButton>();
            newButton.setBatalion(batalion);
        }
    }

    public enum GridSpawnerType
    {
        PLAN,
        BATTALION
    }
}