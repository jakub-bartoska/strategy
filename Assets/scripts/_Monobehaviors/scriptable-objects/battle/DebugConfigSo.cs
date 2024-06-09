using UnityEngine;

namespace _Monobehaviors.scriptable_objects.battle
{
    [CreateAssetMenu(fileName = "config", menuName = "ScriptableObjects/DebugConfigSO")]
    public class DebugConfigSO : ScriptableObject
    {
        [SerializeField] public float speed = 10f;
        [SerializeField] public bool doDamage = true;
        [SerializeField] public float dmgPerSecond = 1f;
    }
}