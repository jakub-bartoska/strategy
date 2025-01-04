using component._common.system_switchers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.army_to_spawn_switcher.common
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
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

            if (systemHolder.ValueRO.desiredStatus == SystemStatus.PRE_BATTLE)
            {
                if (systemHolder.ValueRO.currentStatus == SystemStatus.STRATEGY)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.COMPANY_TO_BATTALION
                    });
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.SPAWN_PRE_BATTLE_TILES
                    });
                }
            }


            if (systemHolder.ValueRO.currentStatus == SystemStatus.PRE_BATTLE)
            {
                if (systemHolder.ValueRO.desiredStatus == SystemStatus.BATTLE)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.SPAWN_ARMY
                    });
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.BATTALION_CARDS_TO_BATTALION
                    });
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.SAVE_BATTALION_POSITIONS_FROM_SO
                    });
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.CLEAN_PRE_BATTLE
                    });
                }
            }

            if (systemHolder.ValueRO.currentStatus == SystemStatus.STRATEGY)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.STOP_STRATEGY_MOVEMENT
                });
            }

            if (systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY)
            {
                if (systemHolder.ValueRO.currentStatus == SystemStatus.MENU)
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.SPAWN_STRATEGY
                    });
                }
                else
                {
                    blockers.Add(new SystemSwitchBlocker
                    {
                        blocker = Blocker.ACTIVATE_STRATEGY_MOVEMENT
                    });
                }
            }

            //todo doresit restart
            if (systemHolder.ValueRO.desiredStatus == SystemStatus.PRE_BATTLE ||
                systemHolder.ValueRO.desiredStatus == SystemStatus.STRATEGY ||
                systemHolder.ValueRO.desiredStatus == SystemStatus.BATTLE)
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

            if (systemHolder.ValueRO.currentStatus == SystemStatus.NO_STATUS &&
                systemHolder.ValueRO.desiredStatus == SystemStatus.PRE_BATTLE)
            {
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.ARMIES_MONO_TO_ENTITY
                });
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.COMPANY_TO_BATTALION
                });
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.LOAD_BATTALION_POSITIONS_FROM_SO
                });
                blockers.Add(new SystemSwitchBlocker
                {
                    blocker = Blocker.SPAWN_PRE_BATTLE_TILES
                });
            }
        }

        private bool containsAutoAddBlockers(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
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