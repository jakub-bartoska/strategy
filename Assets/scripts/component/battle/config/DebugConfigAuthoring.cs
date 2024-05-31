using System;
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
        [SerializeField] private TMP_InputField dmgInput;
        [SerializeField] private DebugConfigSO configSO;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            speedInput.text = configSO.speed.ToString();
            doDamageToggle.isOn = configSO.doDamage;
            dmgInput.text = configSO.dmgPerSecond.ToString();
        }

        public DebugConfig collectData()
        {
            configSO.speed = float.Parse(speedInput.text);
            configSO.doDamage = doDamageToggle.isOn;
            configSO.dmgPerSecond = float.Parse(dmgInput.text);

            return new DebugConfig
            {
                doDamage = configSO.doDamage,
                speed = configSO.speed,
                dmgPerSecond = configSO.dmgPerSecond
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

    [CreateAssetMenu(fileName = "config", menuName = "ScriptableObjects/DebugConfigSO")]
    public class DebugConfigSO : ScriptableObject
    {
        [SerializeField] public float speed = 10f;
        [SerializeField] public bool doDamage = true;
        [SerializeField] public float dmgPerSecond = 1f;
    }
}