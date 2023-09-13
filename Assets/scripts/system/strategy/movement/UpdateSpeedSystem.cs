using component._common.system_switchers;
using component.strategy.general;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;

namespace component.strategy.interactions
{
    public partial struct UpdateSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSteering>();
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var positions = new NativeList<(long, float3)>(300, Allocator.TempJob);
            new CollectAgentsPositionsJob
                {
                    positions = positions
                }.Schedule(state.Dependency)
                .Complete();

            var result = new NativeHashMap<long, float>(positions.Length, Allocator.TempJob);
            foreach (var (id, position) in positions)
            {
                if (NavMesh.SamplePosition(position, out var hit, 1.0f, NavMesh.AllAreas))
                {
                    var res = IndexFromMask(hit.mask);
                    var cost = NavMesh.GetAreaCost(res);
                    result.Add(id, (1f / cost));
                }
            }

            new UpdateAgentSpeedJob
                {
                    idSpeed = result
                }.Schedule(state.Dependency)
                .Complete();
        }

        private int IndexFromMask(int mask)
        {
            for (int i = 0; i < 32; ++i)
            {
                if ((1 << i) == mask)
                    return i;
            }

            return -1;
        }
    }

    public partial struct CollectAgentsPositionsJob : IJobEntity
    {
        public NativeList<(long, float3)> positions;

        private void Execute(AgentSteering _, LocalTransform transform, IdHolder idHolder)
        {
            positions.Add((idHolder.id, transform.Position));
        }
    }

    public partial struct UpdateAgentSpeedJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, float> idSpeed;

        private void Execute(ref AgentSteering agentSteering, IdHolder idHolder)
        {
            var res = idSpeed[idHolder.id];
            agentSteering.Speed = res;
        }
    }
}