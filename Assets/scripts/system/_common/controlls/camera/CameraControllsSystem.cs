using System;
using component._common.camera;
using component._common.config.camera;
using component._common.system_switchers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using utils;

namespace system.controls
{
    public partial struct CameraControllsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SystemStatusHolder>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CameraConfigComponentData>();
            var strategy = state.EntityManager.CreateEntityQuery(typeof(StrategyMapStateMarker));
            var battle = state.EntityManager.CreateEntityQuery(typeof(BattleMapStateMarker));
            state.RequireAnyForUpdate(strategy, battle);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraMovement = InputUtils.getInputs().cameramovement;
            if (cameraMovement.enabled == false) return;

            var systemStatusHolder = SystemAPI.GetSingleton<SystemStatusHolder>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var cameraPosition = Camera.main.transform.position;
            var cameraSpeed = cameraPosition.y * deltaTime;

            var cameraYDelta = cameraMovement.mouseScroll.ReadValue<float>() * cameraSpeed * 10f;
            var cameraXZDelta = new float2(cameraMovement.WASD.ReadValue<Vector2>() * cameraSpeed);

            var config = getCorrectConfig(systemStatusHolder.currentStatus);

            if (cameraYDelta < 0 && config.minValues.y < (cameraPosition.y - config.minValues.y * 0.1))
            {
                var mousePosition =
                    RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>());
                var yDeltaInPercents = math.abs(cameraYDelta / cameraPosition.y);
                var scrollAdjustment = (mousePosition - new float3(cameraPosition)) * yDeltaInPercents;
                cameraXZDelta += scrollAdjustment.xz;
            }

            var newPositionDelta = new float3(cameraXZDelta.x, cameraYDelta, cameraXZDelta.y);

            switch (systemStatusHolder.currentStatus)
            {
                case SystemStatus.BATTLE:
                    var battleCamera = SystemAPI.GetSingletonRW<BattleCamera>();
                    var newBattleCameraPosition =
                        normalizeMovementVector(newPositionDelta, battleCamera.ValueRO.desiredPosition, config);
                    battleCamera.ValueRW.desiredPosition = newBattleCameraPosition;
                    break;
                case SystemStatus.STRATEGY:
                    var strategyCamera = SystemAPI.GetSingletonRW<StrategyCamera>();
                    var newStrategyCameraPosition =
                        normalizeMovementVector(newPositionDelta, strategyCamera.ValueRO.desiredPosition, config);
                    strategyCamera.ValueRW.desiredPosition = newStrategyCameraPosition;
                    break;
                default:
                    throw new Exception("Camera is not supported for this game status");
            }
        }

        private CameraConfigComponentData getCorrectConfig(SystemStatus currentStatus)
        {
            var cameraConfigs = SystemAPI.GetSingletonBuffer<CameraConfigComponentData>();
            foreach (var config in cameraConfigs)
            {
                if (config.gameCameraType == currentStatus)
                {
                    return config;
                }
            }

            throw new Exception("dont have config for this game status : " + currentStatus);
        }

        private float3 normalizeMovementVector(float3 newPositionDelta, float3 oldPosition,
            CameraConfigComponentData config)
        {
            var result = newPositionDelta + oldPosition;
            if (result.x > config.maxValues.x)
            {
                result.x = config.maxValues.x;
            }

            if (result.x < config.minValues.x)
            {
                result.x = config.minValues.x;
            }

            if (result.y > config.maxValues.y)
            {
                result.y = config.maxValues.y;
            }

            if (result.y < config.minValues.y)
            {
                result.y = config.minValues.y;
            }

            if (result.z > config.maxValues.z)
            {
                result.z = config.maxValues.z;
            }

            if (result.z < config.minValues.z)
            {
                result.z = config.minValues.z;
            }

            return result;
        }
    }
}