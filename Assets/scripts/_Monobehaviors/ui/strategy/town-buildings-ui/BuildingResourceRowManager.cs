using System.Collections.Generic;
using component.strategy.player_resources;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingResourceRowManager : MonoBehaviour
    {
        [SerializeField] private GameObject resourceRowPrefab;
        private List<(ResourceHolder, GameObject)> existingRows = new();

        public void addResource(ResourceHolder resourceHolder)
        {
            var newInstance = Instantiate(resourceRowPrefab, transform);
            var rectTransform = newInstance.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.75f - (existingRows.Count * 0.25f));
            rectTransform.anchorMax = new Vector2(1, 1 - (existingRows.Count * 0.25f));

            var buildingResourceRowValues = rectTransform.GetComponent<BuildingResourceRowValues>();
            buildingResourceRowValues.setTexts(resourceHolder.type.ToString(), resourceHolder.value.ToString());
            existingRows.Add((resourceHolder, newInstance));
        }
    }
}