using UnityEngine;

namespace _Monobehaviors
{
    public class MonoBehaviourPrefabHolder : MonoBehaviour
    {
        public static MonoBehaviourPrefabHolder instance;

        public GameObject armyTextPrefab;
        
        public void Awake()
        {
            instance = this;
        }
    }
}