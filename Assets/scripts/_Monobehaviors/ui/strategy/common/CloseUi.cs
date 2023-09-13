using component.strategy.army_components.ui;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui
{
    public class CloseUi : MonoBehaviour
    {
        private EntityManager entityManager;
        private EntityQuery query;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(typeof(InterfaceState));
        }

        public void onClick()
        {
            var interfaceState = query.GetSingletonRW<InterfaceState>();
            interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
            interfaceState.ValueRW.state = UIState.ALL_CLOSED;
        }
    }
}