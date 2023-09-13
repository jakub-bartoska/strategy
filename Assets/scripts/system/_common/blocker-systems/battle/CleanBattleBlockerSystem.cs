using component._common.system_switchers;
using component.general;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.blocker_systems.battle
{
    public partial struct CleanBattleBlockerSystem : ISystem
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

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new CleanupBattleJob()
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.CLEAN_BATTLE)
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
        public partial struct CleanupBattleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Entity entity, BattleCleanupTag tag)
            {
                ecb.DestroyEntity(entity.Index, entity);
            }
        }
    }
}