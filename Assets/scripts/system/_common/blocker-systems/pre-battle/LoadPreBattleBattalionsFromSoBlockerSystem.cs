using _Monobehaviors.scriptable_objects;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace system._common.blocker_systems.battle
{
    [UpdateAfter(typeof(TransformCompaniesToBattalionsBlockerSystem))]
    public partial struct LoadPreBattleBattalionsFromSoBlockerSystem : ISystem
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


            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var soBattalionMap = getBattalionIdToEntityMap();

            for (int i = 0; i < battalionsToSpawn.Length; i++)
            {
                var battalion = battalionsToSpawn[i];
                if (soBattalionMap.TryGetValue(battalion.battalionId, out var soBattalion))
                {
                    Debug.Log("Adding");
                    battalion.position = soBattalion.position;
                    battalionsToSpawn[i] = battalion;
                }
            }

            Debug.Log("part 1");
        }

        private NativeHashMap<long, BattalionToSpawn> getBattalionIdToEntityMap()
        {
            var soBattalions = SOHolder.instance.getBattalionsToSpawn();
            var result = new NativeHashMap<long, BattalionToSpawn>(soBattalions.Length, Allocator.Temp);
            foreach (var battalionToSpawn in soBattalions)
            {
                result.Add(battalionToSpawn.battalionId, battalionToSpawn);
            }

            Debug.Log("tmp 1 count: " + result.Count);

            soBattalions.Dispose();
            return result;
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.LOAD_BATTALION_POSITIONS_FROM_SO)
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