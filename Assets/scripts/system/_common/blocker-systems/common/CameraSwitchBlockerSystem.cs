using _Monobehaviors.camera;
using component._common.general;
using component._common.system_switchers;
using system.battle.battle_finish;
using system.strategy.spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.army_to_spawn_switcher.common
{
    [UpdateAfter(typeof(BattleSpawnerSystem))]
    [UpdateAfter(typeof(StrategySpawnerSystem))]
    [UpdateAfter(typeof(BattleFinishSystem))]
    public partial struct CameraSwitchBlockerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SingletonEntityTag>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsCameraSwitch(blockers)) return;

            var systemStatusHolder = SystemAPI.GetSingleton<SystemStatusHolder>();
            switch (systemStatusHolder.desiredStatus)
            {
                case SystemStatus.BATTLE:
                    CameraManager.instance.SwitchCamera(GameCameraType.BATTLE);
                    //var battleCameraPosition = SystemAPI.GetSingleton<BattleCamera>();
                    //Camera.main.transform.position = battleCameraPosition.desiredPosition;
                    break;
                case SystemStatus.STRATEGY:
                    CameraManager.instance.SwitchCamera(GameCameraType.STRATEGY);
                    //var strategyCameraPosition = SystemAPI.GetSingleton<StrategyCamera>();
                    //Camera.main.transform.position = strategyCameraPosition.desiredPosition;
                    break;
                case SystemStatus.PRE_BATTLE:
                    CameraManager.instance.SwitchCamera(GameCameraType.PRE_BATTLE);
                    break;
                default:
                    return;
            }
        }

        private bool containsCameraSwitch(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.CAMERA_SWITCH)
                {
                    containsArmySpawn = true;
                }
                else
                {
                    blockers.Add(blocker);
                }
            }

            return containsArmySpawn;
        }
    }
}