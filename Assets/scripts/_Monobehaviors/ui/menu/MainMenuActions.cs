using component._common.system_switchers;
using UnityEngine;

namespace _Monobehaviors.ui.menu
{
    public class MainMenuActions : MonoBehaviour
    {
        public void onStartNewGameClicked()
        {
            StateManagerForMonos.getInstance().updateStatusFromMonos(SystemStatus.STRATEGY);
        }

        public void onExitClicked()
        {
#if UNITY_STANDALONE
            Application.Quit();
#endif
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}