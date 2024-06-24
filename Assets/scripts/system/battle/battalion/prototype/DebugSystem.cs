using component._common.system_switchers;
using component.authoring_pairs;
using component.authoring_pairs.PrefabHolder;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.cleanup
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    public partial struct DebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var markedEntitiesCount = SystemAPI.QueryBuilder()
                .WithAll<ArrowMarkerDebug>()
                .Build()
                .CalculateEntityCount();

            if (markedEntitiesCount == 0)
            {
                var entity = ecb.Instantiate(prefabHolder.arrowPrefab);
                var transform = LocalTransform.FromPosition(new float3(10050, 2, 10000));
                transform.Scale = 4f;

                var arrowMarkerDebug = new ArrowMarkerDebug
                {
                    startingPosition = transform.Position,
                    yForce = 20f
                };

                ecb.AddComponent(entity, arrowMarkerDebug);
                ecb.SetComponent(entity, transform);
                return;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;

            new DebugJob
                {
                    deltaTime = deltaTime
                }
                .Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct DebugJob : IJobEntity
    {
        public float deltaTime;

        private void Execute(ref ArrowMarkerDebug arrowMarkerDebug, ref LocalTransform localTransform)
        {
            if (localTransform.Position.y < (arrowMarkerDebug.startingPosition.y - 0.5f))
            {
                return;
            }

            localTransform.Position.x -= 20 * deltaTime;
            localTransform.Position.y += arrowMarkerDebug.yForce * deltaTime;

            arrowMarkerDebug.yForce -= 10 * deltaTime;
        }
    }
}