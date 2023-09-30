using UnityEngine;

namespace _Monobehaviors.minor_ui
{
    public class MinorUi : MonoBehaviour
    {
        public static MinorUi instance;
        [SerializeField] private GameObject minorUi;

        private void Awake()
        {
            instance = this;
        }

        public void changeActive(bool active)
        {
            minorUi.SetActive(active);
        }
    }
}