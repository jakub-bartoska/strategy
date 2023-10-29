using TMPro;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingPrefabData : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI buildingName;
        [SerializeField] private GameObject resourceRowPrefab;

        public void setBuildingName(string name)
        {
            buildingName.text = name;
        }
    }
}