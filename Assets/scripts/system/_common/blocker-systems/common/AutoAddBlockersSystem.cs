using component._common.system_switchers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.army_to_spawn_switcher.common
{
    public partial struct AutoAddBlockersSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SystemSwitchBlocker>();
            state.RequireForUpdate<SystemStatusHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsAutoAddBlockers(blockers)) return;

            var systemHolder = SystemAPI.GetSingletonRW<SystemStatusHolder>();
            if (systemHolder.ValueRO.desiredStatus == SystemStatus.INGAME_MENU)
            {
                if (systemHolder.ValueRO.currentStatus == SystemStatus.STRATEGY)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.STOP_STRATEGY_MOVEMENT
                    });
                }

                if (systemHolder.ValueRO.currentStatus == SystemStatus.BATTLE)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.STOP_BATTLE_MOVEMENT
                    });
                }
            }

            if (systemHolder.ValueRO.currentStatus == SystemStatus.INGAME_MENU)
            {
                if (systemHolder.ValueRO.previousStatus == SystemStatus.STRATEGY)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.ACTIVATE_STRATEGY_MOVEMENT
                    });
                }

                if (systemHolder.ValueRO.previousStatus == SystemStatus.BATTLE)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.ACTIVATE_BATTLE_MOVEMENT
                    });
                }
            }
            else
            {
                if (systemHolder.ValueRO.desiredStatus == SystemStatus.BATTLE)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.SPAWN_ARMY
                    });
                }
            }

            if (systemHolder.ValueRO.currentStatus == SystemStatus.STRATEGY &&
                systemHolder.ValueRO.desiredStatus == SystemStatus.BATTLE)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.STOP_STRATEGY_MOVEMENT
                });
                //spawn armies
            }

            if (systemHolder.ValueRO.currentStatus == SystemStatus.BATTLE &&
                systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.ACTIVATE_STRATEGY_MOVEMENT
                });
                //remove battle entities 
            }

            if (systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY &&
                systemHolder.ValueRO.currentStatus == SystemStatus.MENU)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.SPAWN_STRATEGY
                });
            }

            //todo doresit restart
            if ((systemHolder.ValueRO.desiredStatus == SystemStatus.BATTLE &&
                 systemHolder.ValueRO.currentStatus == SystemStatus.STRATEGY) ||
                (systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY &&
                 systemHolder.ValueRO.currentStatus == SystemStatus.BATTLE) ||
                (systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY &&
                 systemHolder.ValueRO.currentStatus == SystemStatus.MENU))
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.CAMERA_SWITCH
                });
            }

            if (systemHolder.ValueRO.desiredStatus == SystemStatus.RESTART)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.CLEAN_STRATEGY
                });
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.CLEAN_BATTLE
                });
                systemHolder.ValueRW.desiredStatus = SystemStatus.MENU;
            }
        }

        private bool containsAutoAddBlockers(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.AUTO_ADD_BLOCKERS)
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