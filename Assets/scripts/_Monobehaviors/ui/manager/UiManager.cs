using component._common.system_switchers;
using UnityEngine;

namespace _Monobehaviors.ui.manager
{
    public class UiManager : MonoBehaviour
    {
        [SerializeField] private GameObject strategyUi;
        [SerializeField] private GameObject menuUi;
        [SerializeField] private GameObject battleUi;
        [SerializeField] private GameObject ingameMenuUi;

        private void Awake()
        {
            StateManagerForMonos.getInstance().onSystemStatusChanged += onSystemStatusChanged;
        }

        private void onSystemStatusChanged(SystemStatus newStatus, SystemStatus oldStatus)
        {
            var strategy = newStatus == SystemStatus.STRATEGY;
            var menu = newStatus == SystemStatus.MENU;
            var battle = newStatus == SystemStatus.BATTLE;
            var ingameMenu = newStatus == SystemStatus.INGAME_MENU;

            strategyUi.SetActive(strategy);
            menuUi.SetActive(menu);
            battleUi.SetActive(battle);
            ingameMenuUi.SetActive(ingameMenu);
        }
    }
}