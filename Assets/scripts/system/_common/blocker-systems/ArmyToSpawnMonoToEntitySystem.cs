using component;
using component._common.system_switchers;
using component.config.game_settings;
using component.strategy.general;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace system._common.army_to_spawn_switcher
{
    public partial struct ArmyToSpawnMonoToEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyToSpawn>();
            state.RequireForUpdate<ArmyToSpawnMono>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Debug.Log("ArmyToSpawnMonoToEntitySystem");
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsArmyMonoToEntity(blockers)) return;

            Debug.Log("ArmyToSpawnMonoToEntitySystem2");

            var armiesToSpawn = SystemAPI.GetSingletonBuffer<ArmyToSpawn>();
            var armiesToSpawnMono = SystemAPI.GetSingletonBuffer<ArmyToSpawnMono>();
            var random = SystemAPI.GetSingletonRW<GameRandom>();

            if (armiesToSpawnMono.Length != 0)
            {
                var armyId = -1;
                foreach (var armyToSpawnManual in armiesToSpawnMono)
                {
                    armiesToSpawn.Add(new ArmyToSpawn
                    {
                        team = armyToSpawnManual.team,
                        originalArmyType = HolderType.ARMY,
                        armyCompanyId = random.ValueRW.random.NextInt(),
                        distanceBetweenSoldiers = armyToSpawnManual.distanceBetweenSoldiers,
                        count = armyToSpawnManual.count,
                        formation = armyToSpawnManual.formation,
                        armyType = armyToSpawnManual.armyType,
                        originalArmyId = armyId--
                    });
                }
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var bufferEntity = SystemAPI.GetSingletonEntity<ArmyToSpawnMono>();
            ecb.DestroyEntity(bufferEntity);
        }

        private bool containsArmyMonoToEntity(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.ARMIES_MONO_TO_ENTITY)
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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}