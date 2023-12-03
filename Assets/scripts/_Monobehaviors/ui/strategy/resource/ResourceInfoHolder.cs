using System;
using component.strategy.player_resources;
using Unity.Collections;
using UnityEngine;

namespace _Monobehaviors.resource
{
    public class ResourceInfoHolder : MonoBehaviour
    {
        public static ResourceInfoHolder instance;

        [SerializeField] private MapResource mapResource;

        private void Awake()
        {
            instance = this;
        }

        public void updateState(NativeList<ResourceHolder> resources, ResourceTabState state)
        {
            mapResource.changeActive(state == ResourceTabState.OPEN);

            switch (state)
            {
                case ResourceTabState.OPEN:
                    mapResource.updateResources(resources);
                    break;
                case ResourceTabState.CLOSED:
                    break;
                default:
                    throw new Exception("Unknown resource tab state " + state);
            }
        }
    }

    public enum ResourceTabState
    {
        OPEN,
        CLOSED
    }
}