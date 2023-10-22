using UnityEngine;

namespace _Monobehaviors.minor_ui
{
    public class CaravanUi : MonoBehaviour
    {
        public static CaravanUi instance;
        [SerializeField] private GameObject caravanUi;

        private void Awake()
        {
            instance = this;
        }

        public void changeActive(bool active)
        {
            caravanUi.SetActive(active);
        }
    }
}