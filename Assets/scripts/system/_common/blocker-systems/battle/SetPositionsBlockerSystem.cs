using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system
{
    [BurstCompile]
    public partial struct SetPositionsBlockerSystem : ISystem
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
            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.BATTALION_CARDS_TO_BATTALION)
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