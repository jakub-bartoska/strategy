using component;
using component._common.general;
using component._common.movement_agents;
using component._common.system_switchers;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system
{
    public partial struct StopBattleAgentsSystem : ISystem
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

            if (!containsArmySpawn(blockers)) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new StopBattleAgentsJob
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var singletonEntity = SystemAPI.GetSingletonEntity<SingletonEntityTag>();
            ecb.RemoveComponent<AgentMovementAllowedForBattleTag>(singletonEntity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.Temp);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.STOP_BATTLE_MOVEMENT)
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
        public partial struct StopBattleAgentsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(SoldierStatus status, ref AgentBody agentBody, Entity entity,
                ref AgentSteering steering)
            {
                if (agentBody.IsStopped) return;

                agentBody.IsStopped = true;
                ecb.AddComponent<StoppedAgentTag>(status.index, entity);
            }
        }
    }
}