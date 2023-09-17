using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.player_resources
{
    public class WoodUi : MonoBehaviour
    {
        public static WoodUi instance;
        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            instance = this;
        }

        public void updateWood(long gold)
        {
            text.text = gold.ToString();
        }
    }
}