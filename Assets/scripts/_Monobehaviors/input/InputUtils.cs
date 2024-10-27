using _Monobehaviors.ui;
using component._common.system_switchers;

namespace utils
{
    public class InputUtils
    {
        private static InputUtils instance;
        private static bool initialized;
        private BattleInputs battleInputs;

        public static BattleInputs getInputs()
        {
            if (instance == null)
            {
                instance = new InputUtils();
                instance.init();
            }

            return instance.getBattleInputs();
        }

        public BattleInputs getBattleInputs()
        {
            return battleInputs;
        }

        public void init()
        {
            battleInputs = new BattleInputs();
            StateManagerForMonos.getInstance().onSystemStatusChanged += enableInputs;
            enableCommons();
            initialized = true;
        }

        private void enableCommons()
        {
            battleInputs.Enable();
            battleInputs.prebattle.Enable();
            battleInputs.common.Enable();
            battleInputs.strategy.Disable();
            battleInputs.battle.Disable();
            battleInputs.cameramovement.Disable();
        }

        private void enableInputs(SystemStatus newStatus, SystemStatus _)
        {
            if (!initialized)
            {
                instance.init();
            }

            if (newStatus == SystemStatus.STRATEGY)
            {
                battleInputs.strategy.Enable();
            }
            else
            {
                battleInputs.strategy.Disable();
            }

            if (newStatus == SystemStatus.PRE_BATTLE)
            {
                battleInputs.prebattle.Enable();
            }
            else
            {
                battleInputs.prebattle.Disable();
            }

            if (newStatus == SystemStatus.BATTLE)
            {
                battleInputs.battle.Enable();
            }
            else
            {
                battleInputs.battle.Disable();
            }

            if (newStatus == SystemStatus.BATTLE || newStatus == SystemStatus.STRATEGY || newStatus == SystemStatus.PRE_BATTLE)
            {
                battleInputs.cameramovement.Enable();
            }
            else
            {
                battleInputs.cameramovement.Disable();
            }
        }
    }
}