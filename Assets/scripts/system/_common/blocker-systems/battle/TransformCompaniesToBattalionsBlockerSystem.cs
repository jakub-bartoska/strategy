using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using system._common.army_to_spawn_switcher;
using system.battle.utils.pre_battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.blocker_systems.battle
{
    [UpdateAfter(typeof(ArmyToSpawnMonoToEntitySystem))]
    public partial struct TransformCompaniesToBattalionsBlockerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SingletonEntityTag>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SystemSwitchBlocker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();

            if (!containsBlocker(blockers)) return;

            var companiesToSpawn = SystemAPI.GetSingletonBuffer<CompanyToSpawn>();
            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            battalionsToSpawn.Clear();
            foreach (var companyToSpawn in companiesToSpawn)
            {
                var battalionCount = companyToSpawn.count / 10;
                for (var i = 0; i < battalionCount; i++)
                {
                    battalionsToSpawn.Add(new BattalionToSpawn
                    {
                        team = companyToSpawn.team,
                        armyType = companyToSpawn.armyType,
                        count = 10,
                        armyCompanyId = companyToSpawn.armyCompanyId,
                    });
                }

                var lastBattalionSize = companyToSpawn.count % 10;
                if (lastBattalionSize == 0) continue;
                battalionsToSpawn.Add(new BattalionToSpawn
                {
                    team = companyToSpawn.team,
                    armyType = companyToSpawn.armyType,
                    count = lastBattalionSize,
                    armyCompanyId = companyToSpawn.armyCompanyId
                });
            }

            TileSpawner.spawnTiles(prefabHolder, ecb);
        }

        private bool containsBlocker(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.COMPANY_TO_BATTALION)
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