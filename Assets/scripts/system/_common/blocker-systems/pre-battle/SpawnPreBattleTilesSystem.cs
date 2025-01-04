using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.pre_battle.marker;
using system.battle.utils.pre_battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace system._common.blocker_systems.battle
{
    [UpdateAfter(typeof(LoadPreBattleBattalionsFromSoBlockerSystem))]
    public partial struct SpawnPreBattleTilesSystem : ISystem
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

            Debug.Log("part 2");

            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var positionToBattalionMap = getBattalionPositionToEntityMap(battalionsToSpawn);

            var newBattalionCards = TileSpawner.spawnTiles(prefabHolder, state.EntityManager, positionToBattalionMap);

            var preBattle = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
            preBattle.Clear();
            preBattle.AddRange(newBattalionCards);
        }

        private NativeHashMap<float3, BattalionToSpawn> getBattalionPositionToEntityMap(
            DynamicBuffer<BattalionToSpawn> battalions)
        {
            var result = new NativeHashMap<float3, BattalionToSpawn>(battalions.Length, Allocator.Temp);
            foreach (var battalion in battalions)
            {
                if (!battalion.position.HasValue) continue;

                result.Add(battalion.position.Value, battalion);
            }

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
                if (blocker.blocker == Blocker.SPAWN_PRE_BATTLE_TILES)
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