using component.config.game_settings;
using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.army_card
{
    public class ArmyCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI countText;
        private int maxCount;

        public void setTypeText(SoldierType type)
        {
            typeText.text = type.ToString();
        }

        public void setMax(int max)
        {
            maxCount = max;
        }

        public void setCountText(int count)
        {
            countText.text = count + " / " + maxCount;
        }
    }
}