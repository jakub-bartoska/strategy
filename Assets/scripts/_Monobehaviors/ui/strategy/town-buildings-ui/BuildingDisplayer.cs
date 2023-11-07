using System.Collections.Generic;
using component.strategy.buildings;
using component.strategy.player_resources;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingDisplayer : MonoBehaviour
    {
        public static BuildingDisplayer instance;

        [SerializeField] public Transform buildingParent;
        public GameObject buildingPrefab;
        public List<(BuildingType, GameObject, bool)> existingTabs = new();

        public void Awake()
        {
            instance = this;
        }

        public void displayBuilding(BuildingType buildingType, NativeList<ResourceHolder> resources, bool hasTownBuilding)
        {
            if (alreadyExists(buildingType, hasTownBuilding)) return;

            var newInstance = Instantiate(buildingPrefab, buildingParent);
            newInstance.GetComponent<BuildingPrefabData>().setBuildingType(buildingType, hasTownBuilding);
            var rectTransform = newInstance.GetComponent<RectTransform>();
            var y = -100 * existingTabs.Count - 50;
            rectTransform.anchoredPosition = new Vector3(150, y);
            if (hasTownBuilding)
            {
                newInstance.GetComponent<RawImage>().color = new Color(0.7f, 1f, 0.7f, 1f);
            }

            var buildingResourceRowManager = newInstance.GetComponentInChildren<BuildingResourceRowManager>();
            foreach (var row in resources)
            {
                buildingResourceRowManager.addResource(row);
            }

            existingTabs.Add((buildingType, newInstance, hasTownBuilding));
        }

        private bool alreadyExists(BuildingType buildingType, bool hasTownBuilding)
        {
            foreach (var tab in existingTabs)
            {
                if (tab.Item1 == buildingType && tab.Item3 == hasTownBuilding) return true;
            }

            return false;
        }
    }
}