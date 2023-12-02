using System;
using component.config.game_settings;
using UnityEngine;

namespace _Monobehaviors
{
    public class MonoBehaviourPrefabHolder : MonoBehaviour
    {
        public static MonoBehaviourPrefabHolder instance;

        public GameObject armyTextPrefab;
        public Texture swordIcon;
        public Texture archerIcon;
        public Texture cavalryIcon;

        public void Awake()
        {
            instance = this;
        }

        public Texture getIconByType(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.SWORDSMAN:
                    return swordIcon;
                case SoldierType.ARCHER:
                    return archerIcon;
                case SoldierType.HORSEMAN:
                    return cavalryIcon;
                default:
                    throw new Exception("unknown type " + type);
            }
        }
    }
}