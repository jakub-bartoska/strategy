using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.player_resources
{
    public class GoldUi : MonoBehaviour
    {
        public static GoldUi instance;
        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            instance = this;
        }

        public void updateGold(long gold)
        {
            text.text = gold.ToString();
        }
    }
}