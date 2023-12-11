using component._common.general;
using component._common.movement_agents;
using component._common.system_switchers;
using component.strategy.general;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system._common.army_to_spawn_switcher.startegy
{
    public partial struct ActivateStrategyAgentsSystem : ISystem
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

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new ActivateStrategyAgentsJob
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var singletonEntity = SystemAPI.GetSingletonEntity<SingletonEntityTag>();
            ecb.AddComponent<AgentMovementAllowedTag>(singletonEntity);
        }

        private bool containsArmySpawn(DynamicBuffer<SystemSwitchBlocker> blockers)
        {
            if (blockers.Length == 0) return false;

            var oldBufferData = blockers.ToNativeArray(Allocator.TempJob);
            blockers.Clear();
            var containsArmySpawn = false;
            foreach (var blocker in oldBufferData)
            {
                if (blocker.blocker == Blocker.ACTIVATE_STRATEGY_MOVEMENT)
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
        public partial struct ActivateStrategyAgentsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(ref AgentBody agentBody, Entity entity, IdHolder idHolder,
                StoppedAgentTag stoppedAgent)
            {
                ecb.RemoveComponent<StoppedAgentTag>((int) idHolder.id, entity);
                agentBody.IsStopped = false;
            }
        }
    }
}