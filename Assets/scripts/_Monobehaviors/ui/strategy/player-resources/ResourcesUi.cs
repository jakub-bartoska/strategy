using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Monobehaviors.ui.player_resources
{
    public class ResourcesUi : MonoBehaviour
    {
        public static ResourcesUi instance;
        [SerializeField] private List<ResourceLink> resourceValues;

        private void Awake()
        {
            instance = this;
        }

        public void updateResource(ResourceType type, long newValue)
        {
            resourceValues.ForEach(resourceUi =>
            {
                if (resourceUi.type == type)
                {
                    resourceUi.value.text = newValue.ToString();
                }
            });
        }
    }

    public enum ResourceType
    {
        GOLD,
        WOOD,
        FOOD,
        STONE
    }

    [Serializable]
    public class ResourceLink
    {
        public ResourceType type;
        public TextMeshProUGUI value;
    }
}