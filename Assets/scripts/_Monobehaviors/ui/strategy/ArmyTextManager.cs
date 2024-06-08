using System.Collections.Generic;
using component.strategy.ui;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors
{
    public class ArmyTextManager : MonoBehaviour
    {
        private Dictionary<long, TextMeshProUGUI> armyTexts;
        private EntityManager entityManager;
        private EntityQuery query;

        public void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(typeof(StrategyUiLabel));
            armyTexts = new();
        }

        public void Update()
        {
            var labels = query.ToComponentDataArray<StrategyUiLabel>(Allocator.TempJob); //ok
            var labelsToDelete = new List<long>(armyTexts.Keys);
            foreach (var label in labels)
            {
                var position = label.position;
                var vectorPosition = new Vector3(position.x, position.y, position.z) + new Vector3(0, 0, 0.5f);
                if (armyTexts.TryGetValue(label.id, out var tmpg))
                {
                    tmpg.transform.position = vectorPosition;
                    tmpg.text = label.text.ToString();
                }
                else
                {
                    var newInstance = Instantiate(MonoBehaviourPrefabHolder.instance.armyTextPrefab, vectorPosition,
                        Quaternion.Euler(90, 0, 0), transform);
                    var newTmpg = newInstance.GetComponent<TextMeshProUGUI>();
                    newTmpg.text = label.text.ToString();
                    armyTexts.Add(label.id, newTmpg);
                }

                labelsToDelete.Remove(label.id);
            }

            labelsToDelete.ForEach(id =>
            {
                var labelToDestroy = armyTexts[id];
                Destroy(labelToDestroy);
                armyTexts.Remove(id);
            });
            labels.Dispose();
        }
    }
}