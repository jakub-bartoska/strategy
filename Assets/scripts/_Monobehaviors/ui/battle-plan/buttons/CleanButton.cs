using _Monobehaviors.ui.battle_plan.army_card;
using _Monobehaviors.ui.battle_plan.counter;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class CleanButton : MonoBehaviour
    {
        public void onClicked()
        {
            CardManager.instance.clear();
            ArmyFormationManager.instance.clear();
        }
    }
}