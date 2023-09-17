using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.player_resources
{
    public class FoodUi : MonoBehaviour
    {
        public static FoodUi instance;
        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            instance = this;
        }

        public void updateFood(long gold)
        {
            text.text = gold.ToString();
        }
    }
}