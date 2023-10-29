using System.Collections.Generic;
using component.strategy.buildings;
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

        public void Start()
        {
            displayBuilding(BuildingType.BARRACKS);
            displayBuilding(BuildingType.TOWN_HALL);
        }

        public void displayBuilding(BuildingType buildingType)
        {
            var newInstance = Instantiate(buildingPrefab, transform);
            newInstance.GetComponent<BuildingPrefabData>().setBuildingName(buildingType.ToString());
            var rectTransform = newInstance.GetComponent<RectTransform>();
            var y = -100 * existingTabs.Count - 50;
            rectTransform.anchoredPosition = new Vector3(150, y);

            existingTabs.Add((buildingType, newInstance));
        }
    }
}