using component._common.system_switchers;
using UnityEngine;

namespace _Monobehaviors.ui.menu
{
    public class IngameMenuActions : MonoBehaviour
    {
        public void onContinue()
        {
            StateManagerForMonos.getInstance().updateToPreviousStatus();
        }

        public void onRestart()
        {
            StateManagerForMonos.getInstance().updateStatusFromMonos(SystemStatus.RESTART);
        }

        public void onExitToMainMenu()
        {
        }

        public void onExit()
        {
            onContinue();
        }
    }
}