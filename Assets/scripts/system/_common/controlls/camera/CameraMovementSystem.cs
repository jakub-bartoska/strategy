using System;
using component._common.camera;
using component._common.system_switchers;
using system.battle.battle_finish;
using system.strategy.spawner;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace system.controls
{
    [UpdateBefore(typeof(BattleSpawnerSystem))]
    [UpdateBefore(typeof(StrategySpawnerSystem))]
    [UpdateBefore(typeof(BattleFinishSystem))]
    public partial struct CameraMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SystemStatusHolder>();
            state.RequireForUpdate<BattleCamera>();
            state.RequireForUpdate<StrategyCamera>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var systemStatusHolder = SystemAPI.GetSingleton<SystemStatusHolder>();

            if (systemStatusHolder.currentStatus != SystemStatus.STRATEGY &&
                systemStatusHolder.currentStatus != SystemStatus.BATTLE) return;

            if (systemStatusHolder.currentStatus != systemStatusHolder.desiredStatus) return;

            var targetPosition = systemStatusHolder.currentStatus switch
            {
                SystemStatus.BATTLE => SystemAPI.GetSingleton<BattleCamera>().desiredPosition,
                SystemStatus.STRATEGY => SystemAPI.GetSingleton<StrategyCamera>().desiredPosition,
                _ => throw new Exception("not supported game state")
            };

            var deltaTime = SystemAPI.Time.DeltaTime;
            var directionSpeed = targetPosition - (float3) Camera.main.transform.position;
            directionSpeed.y = directionSpeed.y * deltaTime * 7;
            directionSpeed.xz = directionSpeed.xz * deltaTime * 7;

            Camera.main.transform.position += (Vector3) directionSpeed;
        }
    }
}