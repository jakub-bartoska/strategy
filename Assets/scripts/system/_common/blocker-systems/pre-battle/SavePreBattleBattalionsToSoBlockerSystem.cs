using _Monobehaviors.scriptable_objects;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.blocker_systems.battle
{
    [UpdateAfter(typeof(SetPositionsBlockerSystem))]
    public partial struct SavePreBattleBattalionsToSoBlockerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsArmySpawn(blockers)) return;

            var battalionToSpawns = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            SOHolder.instance.saveBattalionsToSpawn(battalionToSpawns);
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.SAVE_BATTALION_POSITIONS_FROM_SO)
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