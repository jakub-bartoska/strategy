using _Monobehaviors.ui.battle_plan.counter;
using component._common.general;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.blocker_systems.battle
{
    public partial struct TransformCompaniesToBatalionsBlockerSystem : ISystem
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
            var batalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            batalionsToSpawn.Clear();
            foreach (var companyToSpawn in companiesToSpawn)
            {
                var batalionCount = companyToSpawn.count / 10;
                for (var i = 0; i < batalionCount; i++)
                {
                    batalionsToSpawn.Add(new BattalionToSpawn
                    {
                        team = companyToSpawn.team,
                        armyType = companyToSpawn.armyType,
                        count = 10,
                        armyCompanyId = companyToSpawn.armyCompanyId,
                    });
                }

                var lastBatalionSize = companyToSpawn.count % 10;
                if (lastBatalionSize == 0) continue;
                batalionsToSpawn.Add(new BattalionToSpawn
                {
                    team = companyToSpawn.team,
                    armyType = companyToSpawn.armyType,
                    count = lastBatalionSize,
                    armyCompanyId = companyToSpawn.armyCompanyId,
                });
            }

            ArmyFormationManager.instance.prepare(batalionsToSpawn.ToNativeArray(Allocator.TempJob));
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