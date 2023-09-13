using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation.Editor
{
    [DisableAutoCreation]
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentGizmosSystemGroup))]
    public partial struct AgentColliderGizmosSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gizmos = GetSingletonRW<GizmosSystem.Singleton>();
            var spatial = GetSingleton<AgentSpatialPartitioningSystem.Singleton>();
            new Job
            {
                Gizmos = gizmos.ValueRW.CreateCommandBuffer().AsParallelWriter(),
                Spatial = spatial,
            }.ScheduleParallel();
        }

        [BurstCompile]
        unsafe partial struct Job : IJobEntity
        {
            public GizmosCommandBuffer.ParallelWriter Gizmos;
            [ReadOnly] public AgentSpatialPartitioningSystem.Singleton Spatial;

            public void Execute(Entity entity, in AgentShape shape, in LocalTransform transform,
                in DrawGizmos drawGizmos)
            {
                var action = new DrawSpatialEntities {Gizmos = Gizmos, Position = transform.Position};
                Spatial.QueryCylinder(transform.Position, shape.Radius, shape.Height, ref action);
                Spatial.QueryCylinderCells(transform.Position, shape.Radius, shape.Height,
                    new DrawSpatialBoxes {Gizmos = Gizmos});
            }
        }

        struct DrawSpatialBoxes : ISpatialQueryVolume
        {
            public GizmosCommandBuffer.ParallelWriter Gizmos;

            public void Execute(float3 position, float3 size)
            {
                Gizmos.DrawWireBox(position, size, new UnityEngine.Color(0, 0, 1, 0.4f));
            }
        }

        struct DrawSpatialEntities : ISpatialQueryEntity
        {
            public GizmosCommandBuffer.ParallelWriter Gizmos;
            public float3 Position;

            public void Execute(Entity entity, AgentBody body, AgentShape shape, LocalTransform transform)
            {
                Gizmos.DrawLine(transform.Position, Position, UnityEngine.Color.blue);
            }
        }
    }
}