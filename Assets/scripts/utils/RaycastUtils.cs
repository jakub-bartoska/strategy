using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace utils
{
    public class RaycastUtils
    {
        public static float3 getCurrentMousePosition(RefRW<PhysicsWorldSingleton> physicsWorldSingleton)
        {
            var world = physicsWorldSingleton.ValueRW.PhysicsWorld;

            var mousePosition = Input.mousePosition;
            var unityRay = Camera.main.ScreenPointToRay(mousePosition);

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