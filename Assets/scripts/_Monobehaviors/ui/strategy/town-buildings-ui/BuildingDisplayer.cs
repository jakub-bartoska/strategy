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
        public List<(BuildingType, GameObject, bool)> existingTabs = new(20);

        public void Awake()
        {
            instance = this;
        }

        public void displayBuilding(BuildingType buildingType, NativeList<ResourceHolder> resources, bool hasTownBuilding)
        {
            var tabExists = alreadyExists(buildingType);

            if (tabExists)
            {
                if (hasDifferentData(buildingType, hasTownBuilding))
                {
                    updateTab(buildingType, resources, hasTownBuilding);
                }

                return;
            }

            createTab(buildingType, resources, hasTownBuilding, existingTabs.Count);
        }

        private void createTab(BuildingType buildingType, NativeList<ResourceHolder> resources, bool hasTownBuilding, int index)
        {
            var newInstance = Instantiate(buildingPrefab, buildingParent);
            newInstance.GetComponent<BuildingPrefabData>().setBuildingType(buildingType, hasTownBuilding);
            var rectTransform = newInstance.GetComponent<RectTransform>();
            var y = -100 * index - 50;
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

            if (index == existingTabs.Count)
                existingTabs.Add((buildingType, newInstance, hasTownBuilding));
            else
                existingTabs[index] = (buildingType, newInstance, hasTownBuilding);
        }

        private bool alreadyExists(BuildingType buildingType)
        {
            foreach (var tab in existingTabs)
            {
                if (tab.Item1 == buildingType) return true;
            }

            return false;
        }

        private bool hasDifferentData(BuildingType buildingType, bool hasTownBuilding)
        {
            var tab = existingTabs.Find(x => x.Item1 == buildingType);

            return tab.Item1 != buildingType || tab.Item3 != hasTownBuilding;
        }

        private void updateTab(BuildingType buildingType, NativeList<ResourceHolder> resources, bool hasTownBuilding)
        {
            var tabIndex = 0;
            for (int i = 0; i < existingTabs.Count; i++)
            {
                if (existingTabs[i].Item1 == buildingType)
                {
                    tabIndex = i;
                    break;
                }
            }

            var tab = existingTabs[tabIndex];
            Destroy(tab.Item2);
            createTab(buildingType, resources, hasTownBuilding, tabIndex);
        }
    }
}