using _Monobehaviors.camera;
using component._common.system_switchers;
using component.pre_battle.marker;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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

            //todo unregister events pri zmene game state
            inputs.prebattle.MouseLeftClick.started += _ => leftClickStarted(MarkerType.ADD);
            inputs.prebattle.MouseLeftClick.canceled += _ => leftClickFinished(MarkerType.ADD);

            inputs.prebattle.MouseRightClick.started += _ => leftClickStarted(MarkerType.REMOVE);
            inputs.prebattle.MouseRightClick.canceled += _ => leftClickFinished(MarkerType.REMOVE);
        }


        private void leftClickStarted(MarkerType markerType)
        {
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            preBattleMarker.ValueRW.startPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.state = PreBattleMarkerState.INIT;
            preBattleMarker.ValueRW.MarkerType = markerType;
        }

        private void leftClickFinished(MarkerType markerType)
        {
            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
            preBattleMarker.ValueRW.state = PreBattleMarkerState.FINISHED;
            preBattleMarker.ValueRW.MarkerType = markerType;
        }

        protected override void OnUpdate()
        {
            var change = (int) inputs.cameramovement.mouseScroll.ReadValue<float>();
            updateCamer(change);


            var preBattleMarker = SystemAPI.GetSingletonRW<PreBattlePositionMarker>();
            if (preBattleMarker.ValueRW.state != PreBattleMarkerState.RUNNING)
            {
                return;
            }

            var mousePosition = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.PRE_BATTLE);
            preBattleMarker.ValueRW.endPosition = new float2(mousePosition.x, mousePosition.z);
        }

        private void updateCamer(int cameraYDelta)
        {
            if (cameraYDelta == 0) return;

            var camera = CameraManager.instance.getCamera(GameCameraType.PRE_BATTLE);

            var normalizedSize = normalizeSize((int) camera.orthographicSize + cameraYDelta);
            camera.orthographicSize = normalizedSize;
        }

        private int normalizeSize(int newSize)
        {
            if (newSize < 5)
            {
                return 5;
            }

            if (newSize > 20)
            {
                return 20;
            }

            return newSize;
        }
    }
}