using TMPro;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingPrefabData : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI buildingName;

        public void setBuildingName(string name)
        {
            buildingName.text = name;
        }
    }
}