using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using component.battle.battalion.shadow;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.shadow
{
    public partial struct ShadowFollowBattalion : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
            state.RequireForUpdate<BattalionShadowMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var battalionPositions = new NativeParallelMultiHashMap<long, float3>(1000, Allocator.TempJob);
            new CollectBattalionPositions
                {
                    battalionPositions = battalionPositions.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            if (battalionPositions.Count() == 0) return;

            new UpdateShadowPositions
                {
                    battalionPositions = battalionPositions
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(ChangeRow))]
        public partial struct CollectBattalionPositions : IJobEntity
        {
            public NativeParallelMultiHashMap<long, float3>.ParallelWriter battalionPositions;

            private void Execute(BattalionMarker battalionMarker, LocalTransform localTransform)
            {
                battalionPositions.Add(battalionMarker.id, localTransform.Position);
            }
        }

        [BurstCompile]
        public partial struct UpdateShadowPositions : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<long, float3> battalionPositions;

            private void Execute(BattalionShadowMarker battalionShadowMarker, ref LocalTransform localTransform)
            {
                foreach (var battalionPosition in battalionPositions.GetValuesForKey(battalionShadowMarker.parentBattalionId))
                {
                    var newPosition = new float3(battalionPosition.x, localTransform.Position.y, localTransform.Position.z);
                    localTransform.Position = newPosition;
                }
            }
        }
    }
}