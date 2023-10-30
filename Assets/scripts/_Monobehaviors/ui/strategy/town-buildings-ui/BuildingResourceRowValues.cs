using TMPro;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class BuildingResourceRowValues : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resourceType;
        [SerializeField] private TextMeshProUGUI resourceValue;

        public void setTexts(string type, string value)
        {
            resourceType.text = type;
            resourceValue.text = value;
        }
    }
}