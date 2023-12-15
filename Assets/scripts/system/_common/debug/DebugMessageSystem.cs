using component._common.system_switchers;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace system._common.debug
{
    public partial struct DebugMessageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SystemStatusHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var systemStatusHolder = SystemAPI.GetSingleton<SystemStatusHolder>();
            Debug.Log(systemStatusHolder.currentStatus);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}