using _Monobehaviors.ui;
using component._common.system_switchers;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class StartButtonGroup : MonoBehaviour
    {
        public static StartButtonGroup instance;
        private Button clearButton;
        private VisualElement root;

        private Button startButton;

        private void Awake()
        {
            instance = this;
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            startButton = root.Q<Button>("start-button");
            clearButton = root.Q<Button>("clear-battle-button");

            startButton.RegisterCallback<ClickEvent>(_ => onStartClicked());
            clearButton.RegisterCallback<ClickEvent>(_ => onCleanClicked());
        }

        private void onStartClicked()
        {
            StateManagerForMonos.getInstance().updateStatusFromMonos(SystemStatus.BATTLE);
        }

        private void onCleanClicked()
        {
        }
    }
}