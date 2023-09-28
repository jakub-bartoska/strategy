using System.Collections.Generic;
using _Monobehaviors.ui.player_resources;
using component.strategy.player_resources;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace _Monobehaviors.resource
{
    public class TownResource : MonoBehaviour
    {
        public static TownResource instance;

        [SerializeField] private GameObject row;
        private float firstRowOffset = 20;

        private Dictionary<ResourceType, (long, GameObject, TextMeshProUGUI)> resourceTabs = new();
        private float rowHeight = 30;

        private float wrapperHight = 75;

        private void Awake()
        {
            instance = this;
        }

        public void updateResources(NativeList<ResourceHolder> resources)
        {
            foreach (var resourceHolder in resources)
            {
                if (resourceTabs.TryGetValue(resourceHolder.type, out var value))
                {
                    if (value.Item1 == resourceHolder.value) continue;

                    value.Item3.text = resourceHolder.value.ToString();
                }
                else
                {
                    instantiatenewRow(resourceHolder);
                }
            }
        }

        private void instantiatenewRow(ResourceHolder resourceHolder)
        {
            var newRowY = wrapperHight - (firstRowOffset + rowHeight * resourceTabs.Count);
            var newRow = Instantiate(row, transform);
            newRow.transform.localPosition = new Vector3(0, newRowY, 0);
            var label = newRow.gameObject.GetComponentsInChildren<TextMeshProUGUI>()[0];
            var value = newRow.gameObject.GetComponentsInChildren<TextMeshProUGUI>()[1];
            label.text = resourceHolder.type.ToString();
            value.text = resourceHolder.value.ToString();
            resourceTabs.Add(resourceHolder.type, (resourceHolder.value, newRow, value));
        }
    }
}