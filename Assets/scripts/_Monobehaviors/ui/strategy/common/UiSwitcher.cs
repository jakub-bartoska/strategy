using System;
using component.strategy.army_components.ui;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui
{
    public class UiSwitcher : MonoBehaviour
    {
        private EntityManager entityManager;
        private EntityQuery query;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(typeof(InterfaceState));
        }

        public void closeUi()
        {
            switchUi(UIState.ALL_CLOSED);
        }

        public void switchUi(string newUiState)
        {
            if (UIState.TryParse(newUiState, out UIState uiState))
            {
                switchUi(uiState);
            }
            else
            {
                throw new Exception("unknown state " + newUiState);
            }
        }

        private void switchUi(UIState newUiState)
        {
            var interfaceState = query.GetSingletonRW<InterfaceState>();
            interfaceState.ValueRW.oldState = interfaceState.ValueRO.state;
            interfaceState.ValueRW.state = newUiState;
        }
    }
}