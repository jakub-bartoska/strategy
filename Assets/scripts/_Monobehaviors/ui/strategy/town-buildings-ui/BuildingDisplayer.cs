using System.Collections.Generic;
using component.strategy.buildings;
using component.strategy.player_resources;
using Unity.Collections;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingDisplayer : MonoBehaviour
    {
        public static BuildingDisplayer instance;
        public GameObject buildingPrefab;
        public List<(BuildingType, GameObject)> existingTabs = new();

        public void Awake()
        {
            instance = this;
        }

        public void displayBuilding(BuildingType buildingType, NativeList<ResourceHolder> resources)
        {
            if (alreadyExists(buildingType)) return;

            var newInstance = Instantiate(buildingPrefab, transform);
            newInstance.GetComponent<BuildingPrefabData>().setBuildingName(buildingType.ToString());
            var rectTransform = newInstance.GetComponent<RectTransform>();
            var y = -100 * existingTabs.Count - 50;
            rectTransform.anchoredPosition = new Vector3(150, y);

            var buildingResourceRowManager = newInstance.GetComponentInChildren<BuildingResourceRowManager>();
            foreach (var row in resources)
            {
                buildingResourceRowManager.addResource(row);
            }

            existingTabs.Add((buildingType, newInstance));
        }

        private bool alreadyExists(BuildingType buildingType)
        {
            foreach (var tab in existingTabs)
            {
                if (tab.Item1 == buildingType) return true;
            }

            return false;
        }
    }
}