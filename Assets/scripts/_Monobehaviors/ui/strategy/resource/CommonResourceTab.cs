using System.Collections.Generic;
using _Monobehaviors.ui.player_resources;
using component.strategy.player_resources;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace _Monobehaviors.resource
{
    public class CommonResourceTab : MonoBehaviour
    {
        [SerializeField] private GameObject row;
        private float firstRowOffset = 20;

        private Dictionary<ResourceType, (long, GameObject)> resourceTabs = new();
        private float rowHeight = 30;

        private float wrapperHight = 75;

        public void updateResources(NativeList<ResourceHolder> resources)
        {
            if (!needsRedraw(resources)) return;

            destroyAllRows();

            foreach (var resourceHolder in resources)
            {
                instantiatenewRow(resourceHolder);
            }
        }

        private bool needsRedraw(NativeList<ResourceHolder> resources)
        {
            if (!resources.Length.Equals(resourceTabs.Count)) return true;

            foreach (var resourceHolder in resources)
            {
                if (resourceTabs.TryGetValue(resourceHolder.type, out var value))
                {
                    if (value.Item1 != resourceHolder.value) return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
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
            resourceTabs.Add(resourceHolder.type, (resourceHolder.value, newRow));
        }

        private void destroyAllRows()
        {
            foreach (var keyValuePair in resourceTabs)
            {
                Destroy(keyValuePair.Value.Item2);
            }

            resourceTabs.Clear();
        }
    }
}