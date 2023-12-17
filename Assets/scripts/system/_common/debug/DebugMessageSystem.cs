using component._common.system_switchers;
using Unity.Burst;
using Unity.Entities;

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
            //state.Enabled = false;
            //var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();
//
            //var systemStatusHolder = SystemAPI.GetSingleton<SystemStatusHolder>();
            //Debug.Log(blockers.Length + "      ");
            //if (blockers.Length > 0)
            //{
            //    Debug.Log(blockers[0].blocker);
            //}
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}