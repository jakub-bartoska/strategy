using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.player_resources
{
    public class StoneUi : MonoBehaviour
    {
        public static StoneUi instance;
        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            instance = this;
        }

        public void updateStone(long gold)
        {
            text.text = gold.ToString();
        }
    }
}