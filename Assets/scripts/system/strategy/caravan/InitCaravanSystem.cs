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
            var townTeamPositions = new NativeList<(Team, LocalTransform, long)>(200, Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, transform, team, idHolder) in SystemAPI.Query<TownTag, LocalTransform, TeamComponent, IdHolder>())
            {
                townTeamPositions.Add((team.team, transform, idHolder.id));
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
            [ReadOnly] public NativeList<(Team, LocalTransform, long)> townTeamPositions;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(InitCaravanSetting _, ref AgentBody agent, LocalTransform localTransform, TeamComponent teamComponent, Entity entity)
            {
                var closestPositionId = (new float3(), -1L);
                var smallestDistance = -1f;
                foreach (var (team, transform, townId) in townTeamPositions)
                {
                    if (team != teamComponent.team) continue;

                    var distance = math.distance(transform.Position, localTransform.Position);

                    if (smallestDistance < 0 || smallestDistance > distance)
                    {
                        smallestDistance = distance;
                        closestPositionId = (transform.Position, townId);
                    }
                }

                if (smallestDistance < 0) return;

                agent.Destination = closestPositionId.Item1;
                agent.IsStopped = false;
                ecb.RemoveComponent<InitCaravanSetting>((int) localTransform.Position.x, entity);
                ecb.AddComponent((int) localTransform.Position.x + 1000, entity, new CaravanTarget
                {
                    targetId = closestPositionId.Item2
                });
                ecb.AddComponent((int) localTransform.Position.x + 10000, entity, new CaravanTag());
            }
        }
    }
}