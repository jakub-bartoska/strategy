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

        private float speed = 10f;

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
                speed = speed,
                dmgPerSecond = 1f
            };
        }
    }

    public struct DebugConfig : IComponentData
    {
        public float speed;

        public bool doDamage;

        //damage delaed by 1 fighting soldier (1 unit has 10 soldiers) / 1 sec
        public float dmgPerSecond;
    }
}