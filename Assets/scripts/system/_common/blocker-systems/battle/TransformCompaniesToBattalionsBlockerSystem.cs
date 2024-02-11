using _Monobehaviors.ui.battle_plan.counter;
using component._common.general;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.blocker_systems.battle
{
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

            ArmyFormationManager.instance.prepare(battalionsToSpawn.ToNativeArray(Allocator.TempJob));
        }

        private bool containsBlocker(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
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