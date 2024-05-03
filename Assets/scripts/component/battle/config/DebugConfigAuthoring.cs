using TMPro;
using Unity.Entities;
using UnityEngine;
using Toggle = UnityEngine.UI.Toggle;

namespace component.battle.config
{
    public class DebugConfigAuthoring : MonoBehaviour
    {
        public static DebugConfigAuthoring instance;

        [SerializeField] private TMP_InputField speedInput;
        [SerializeField] private Toggle doDamageToggle;
        private bool doDamage = true;

        private float speed = 1f;

        private void Awake()
        {
            instance = this;
        }

        public DebugConfig collectData()
        {
            speed = float.Parse(speedInput.text);
            doDamage = doDamageToggle.isOn;

            return new DebugConfig
            {
                doDamage = doDamage,
                speed = speed
            };
        }
    }

    public struct DebugConfig : IComponentData
    {
        public float speed;
        public bool doDamage;
    }
}