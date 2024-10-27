using _Monobehaviors.camera;
using component._common.system_switchers;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace system.pre_battle.inputs
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class PreBattleCameraMovementSystem : SystemBase
    {
        private BattleInputs inputs;

        protected override void OnCreate()
        {
            RequireForUpdate<PreBattleMarker>();
            inputs = InputUtils.getInputs();
            ;
        }

        protected override void OnUpdate()
        {
            updateCamerY();
            moveCamera();
        }

        private void moveCamera()
        {
            var cameraChange = new float2(inputs.cameramovement.WASD.ReadValue<Vector2>());
            if (cameraChange.x == 0 && cameraChange.y == 0) return;

            var preBattleCamera = CameraManager.instance.getCamera(GameCameraType.PRE_BATTLE);

            var y = preBattleCamera.orthographicSize;
            var yModifier = y / 5;
            var adjustedBySpeed = cameraChange * 0.025f * yModifier;

            preBattleCamera.transform.position += new Vector3(adjustedBySpeed.x, 0, adjustedBySpeed.y);
        }

        private void updateCamerY()
        {
            var cameraYDelta = (int) inputs.cameramovement.mouseScroll.ReadValue<float>();

            if (cameraYDelta == 0) return;

            var preBattleCamera = CameraManager.instance.getCamera(GameCameraType.PRE_BATTLE);

            var normalizedSize = normalizeSize((int) preBattleCamera.orthographicSize + cameraYDelta);
            preBattleCamera.orthographicSize = normalizedSize;
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