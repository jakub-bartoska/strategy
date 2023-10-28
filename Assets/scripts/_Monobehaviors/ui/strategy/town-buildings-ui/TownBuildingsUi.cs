using UnityEngine;

namespace _Monobehaviors.town_buildings_ui
{
    public class TownBuildingsUi : MonoBehaviour
    {
        public static TownBuildingsUi instance;

        [SerializeField] private GameObject townBuildingUi;

        public void Awake()
        {
            instance = this;
        }

        public void changeActive(bool targetState)
        {
            townBuildingUi.SetActive(targetState);
        }
    }
}