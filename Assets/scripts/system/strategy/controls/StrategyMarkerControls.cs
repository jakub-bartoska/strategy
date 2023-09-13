using _Monobehaviors;
using component._common.movement_agents;
using component._common.system_switchers;
using component.strategy.army_components.ui;
using component.strategy.general;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using utils;

namespace system.strategy.controls
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class StrategyMarkerControls : SystemBase
    {
        private BattleInputs inputs;

        protected override void OnCreate()
        {
            RequireForUpdate<SelectionMarkerState>();
            RequireForUpdate<PhysicsWorldSingleton>();
            RequireForUpdate<StrategyMapStateMarker>();
            RequireForUpdate<AgentMovementAllowedTag>();
            inputs = InputUtils.getInputs();
            inputs.strategy.MouseLeftClick.started += started;
            inputs.strategy.MouseLeftClick.canceled += canceled;
        }

        protected override void OnUpdate()
        {
            var marker = SystemAPI.GetSingletonRW<SelectionMarkerState>();

            if (marker.ValueRO.state != MarkerState.RUNNING || !inputs.strategy.MouseLeftClick.inProgress)
            {
                return;
            }

            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>());
            var mousePosition2D = Input.mousePosition;

            marker.ValueRW.max = mousePosition;
            marker.ValueRW.max2D = new float2(mousePosition2D.x, mousePosition2D.y);
            redrawSelectionMarker(marker.ValueRO);
        }

        private void started(InputAction.CallbackContext ctx)
        {
            if (!SystemAPI.TryGetSingleton<InterfaceState>(out var interfaceState))
            {
                return;
            }

            if (interfaceState.state == UIState.ARMY_UI && Input.mousePosition.y < 150)
            {
                return;
            }

            if (interfaceState.state == UIState.TOWN_UI)
            {
                return;
            }

            var marker = SystemAPI.GetSingletonRW<SelectionMarkerState>();
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>());
            var mousePosition2D = Input.mousePosition;

            marker.ValueRW.min = mousePosition;
            marker.ValueRW.max = mousePosition;
            marker.ValueRW.min2D = new float2(mousePosition2D.x, mousePosition2D.y);
            marker.ValueRW.max2D = new float2(mousePosition2D.x, mousePosition2D.y);
            marker.ValueRW.state = MarkerState.RUNNING;
            redrawSelectionMarker(marker.ValueRO);
            SelectorVisualiser.instance.image.SetActive(true);
        }

        private void canceled(InputAction.CallbackContext ctx)
        {
            if (!SystemAPI.TryGetSingletonRW<SelectionMarkerState>(out var marker))
            {
                return;
            }

            if (marker.ValueRO.state != MarkerState.RUNNING)
            {
                return;
            }

            SelectorVisualiser.instance.image.SetActive(false);
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>());
            var mousePosition2D = Input.mousePosition;

            marker.ValueRW.state = MarkerState.FINISHED;
            marker.ValueRW.max = mousePosition;
            marker.ValueRW.max2D = new float2(mousePosition2D.x, mousePosition2D.y);
        }

        private void redrawSelectionMarker(SelectionMarkerState markerState)
        {
            var position2D = (markerState.min2D + markerState.max2D) / 2;
            var position = new Vector3(position2D.x, position2D.y, 0);
            var size = math.abs(markerState.min2D - markerState.max2D);
            SelectorVisualiser.instance.rectTransform.position = position;
            SelectorVisualiser.instance.rectTransform.sizeDelta = size;
        }
    }
}