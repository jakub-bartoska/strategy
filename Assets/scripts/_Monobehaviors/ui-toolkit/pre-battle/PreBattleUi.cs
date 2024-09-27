using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class PreBattleUi : MonoBehaviour
    {
        private VisualElement root;
        private VisualElement soldierTypeCard;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            var soldierTypeCard = root.Q<VisualElement>("soldier-type");
            var footer = root.Q<VisualElement>("footer");
            soldierTypeCard.visualTreeAssetSource.CloneTree();
            //var newOne = new VisualElement(this.soldierTypeCard);
            //soldierTypeCard.
            //var button = new Button(() => Debug.Log("Button clicked"));
            //button.text = "Click me!";
            //rootVisualElement.Add(button);
        }
    }
}