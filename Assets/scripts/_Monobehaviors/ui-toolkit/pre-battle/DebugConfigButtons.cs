using component.battle.config;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class DebugConfigButtons : MonoBehaviour
    {
        public static DebugConfigButtons instance;
        private TextField battalionSpeed;
        private TextField damage;
        private Toggle doDamage;
        private VisualElement root;

        private void Awake()
        {
            instance = this;
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            battalionSpeed = root.Q<TextField>("battalion-speed-tf");
            damage = root.Q<TextField>("damage-amount-tf");
            doDamage = root.Q<Toggle>("do-damage-toggle");
        }

        public DebugConfig createDebugConfig()
        {
            return new DebugConfig
            {
                doDamage = doDamage.value,
                speed = float.Parse(battalionSpeed.text),
                dmgPerSecond = float.Parse(damage.value)
            };
        }
    }
}