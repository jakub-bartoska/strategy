using component;
using component._common.system_switchers;
using component.strategy.caravan;
using component.strategy.general;
using component.strategy.town_components;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.minors
{
    public partial struct InitCaravanSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<InitCaravanSetting>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var townTeamPositions = new NativeList<(Team, LocalTransform)>(200, Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, transform, team) in SystemAPI.Query<TownTag, LocalTransform, TeamComponent>())
            {
                townTeamPositions.Add((team.team, transform));
            }

            new CollectMarkedTownResources
                {
                    townTeamPositions = townTeamPositions,
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        public partial struct CollectMarkedTownResources : IJobEntity
        {
            [ReadOnly] public NativeList<(Team, LocalTransform)> townTeamPositions;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(InitCaravanSetting _, ref AgentBody agent, LocalTransform localTransform, TeamComponent teamComponent, Entity entity)
            {
                var closestPosition = new float3();
                var smallestDistance = -1f;
                foreach (var (team, transform) in townTeamPositions)
                {
                    if (team != teamComponent.team) continue;

                    var distance = math.distance(transform.Position, localTransform.Position);

                    if (smallestDistance < 0 || smallestDistance > distance)
                    {
                        smallestDistance = distance;
                        closestPosition = transform.Position;
                    }
                }

                if (smallestDistance < 0) return;

                agent.Destination = closestPosition;
                agent.IsStopped = false;
                ecb.RemoveComponent<InitCaravanSetting>((int) localTransform.Position.x, entity);
            }
        }
    }
}