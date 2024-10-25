using _Monobehaviors.camera;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace utils
{
    public class RaycastUtils
    {
        public static float3 getCurrentMousePosition(RefRW<PhysicsWorldSingleton> physicsWorldSingleton, GameCameraType cameraType = GameCameraType.STRATEGY)
        {
            var world = physicsWorldSingleton.ValueRW.PhysicsWorld;

            var mousePosition = Input.mousePosition;
            var unityRay = CameraManager.instance.getCamera(cameraType).ScreenPointToRay(mousePosition);

            var rayInput = new RaycastInput
            {
                Start = unityRay.origin,
                End = unityRay.origin + unityRay.direction.normalized * 300,
                Filter = CollisionFilter.Default,
            };

            world.CastRay(rayInput, out var rayResult);
            return rayResult.Position;
        }
    }
}