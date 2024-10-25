using _Monobehaviors.camera;
using component._common.system_switchers;
using component.pre_battle.marker;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.InputSystem;
using utils;

namespace system.pre_battle.inputs
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class PreBattleInputs : SystemBase
    {
        private BattleInputs inputs;

        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsWorldSingleton>();
            RequireForUpdate<PreBattleMarker>();
            inputs = InputUtils.getInputs();
            inputs.prebattle.MouseLeftClick.started += leftClickStarted;
            inputs.prebattle.MouseLeftClick.canceled += leftClickFinished;
        }


        private void leftClickStarted(InputAction.CallbackContext ctx)
        {
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            preBattleMarker.ValueRW.startPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.state = PreBattleMarkerState.RUNNING;
        }

        private void leftClickFinished(InputAction.CallbackContext ctx)
        {
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.state = PreBattleMarkerState.FINISHED;
        }

        protected override void OnUpdate()
        {
            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            if (preBattleMarker.ValueRW.state != PreBattleMarkerState.RUNNING)
            {
                return;
            }

            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
        }
    }
}