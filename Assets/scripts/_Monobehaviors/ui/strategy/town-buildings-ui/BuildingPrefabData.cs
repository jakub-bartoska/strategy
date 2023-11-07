using _Monobehaviors.town_buildings_ui.town_buy;
using component.strategy.buildings;
using TMPro;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingPrefabData : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI buildingName;
        private bool bought;
        private BuildingType buildingType;

        public void setBuildingType(BuildingType type, bool bought)
        {
            this.bought = bought;
            buildingType = type;
            buildingName.text = type.ToString();
        }

        public void clicked()
        {
            if (!bought)
            {
                BuildingBuyer.instance.buyBuilding(buildingType);
            }
        }
    }
}