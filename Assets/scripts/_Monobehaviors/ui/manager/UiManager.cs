using component._common.system_switchers;
using UnityEngine;

namespace _Monobehaviors.ui.manager
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager instance;

        [SerializeField] private GameObject strategyUi;
        [SerializeField] private GameObject menuUi;
        [SerializeField] private GameObject battleUi;
        [SerializeField] private GameObject ingameMenuUi;
        [SerializeField] private GameObject battlePlanUi;

        private void Awake()
        {
            StateManagerForMonos.getInstance().onSystemStatusChanged += onSystemStatusChanged;
            instance = this;
        }

        private void onSystemStatusChanged(SystemStatus newStatus, SystemStatus oldStatus)
        {
            var strategy = newStatus == SystemStatus.STRATEGY;
            var menu = newStatus == SystemStatus.MENU;
            var battle = newStatus == SystemStatus.BATTLE;
            var ingameMenu = newStatus == SystemStatus.INGAME_MENU;
            var battlePlan = newStatus == SystemStatus.BATTLE_PLAN;

            strategyUi.SetActive(strategy);
            menuUi.SetActive(menu);
            battleUi.SetActive(battle);
            ingameMenuUi.SetActive(ingameMenu);
            battlePlanUi.SetActive(battlePlan);
        }

        public Transform getBattlePlanUiTransform()
        {
            return battlePlanUi.transform;
        }
    }
}