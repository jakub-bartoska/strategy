using component;
using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.split
{
    public partial struct RemoveWaitTagSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var waitingBattalionSoldiers = new NativeParallelMultiHashMap<long, (long, float3)>(500, Allocator.TempJob);
            new CollectWaitingBattalions
                {
                    waitingBattalionSoldiers = waitingBattalionSoldiers.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var battalionsReadyForTagRemoval = new NativeParallelHashSet<long>(500, Allocator.TempJob);

            new CollectBattalionsReadyForTagRemovalJob
                {
                    waitingBattalionSoldiers = waitingBattalionSoldiers,
                    battalionsReadyForTagRemoval = battalionsReadyForTagRemoval.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new TagRemovalJob
                {
                    battalionsReadyForTagRemoval = battalionsReadyForTagRemoval,
                    ecb = ecb.AsParallelWriter()
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectWaitingBattalions : IJobEntity
        {
            public NativeParallelMultiHashMap<long, (long, float3)>.ParallelWriter waitingBattalionSoldiers;

            private void Execute(BattalionMarker battalionMarker, WaitForSoldiers wait, DynamicBuffer<BattalionSoldiers> soldiers)
            {
                foreach (var soldier in soldiers)
                {
                    waitingBattalionSoldiers.Add(soldier.soldierId, (battalionMarker.id, soldier.position));
                }
            }
        }

        [BurstCompile]
        public partial struct CollectBattalionsReadyForTagRemovalJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<long, (long, float3)> waitingBattalionSoldiers;
            public NativeParallelHashSet<long>.ParallelWriter battalionsReadyForTagRemoval;

            private void Execute(SoldierStatus soldierStatus, LocalTransform transform)
            {
                if (waitingBattalionSoldiers.TryGetFirstValue(soldierStatus.index, out var battalion, out var _))
                {
                    if (math.abs(transform.Position.x - battalion.Item2.x) < 0.1f)
                    {
                        battalionsReadyForTagRemoval.Add(battalion.Item1);
                    }
                }
            }
        }

        [BurstCompile]
        public partial struct TagRemovalJob : IJobEntity
        {
            public NativeParallelHashSet<long> battalionsReadyForTagRemoval;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(BattalionMarker soldierStatus, WaitForSoldiers wait, Entity entity)
            {
                if (battalionsReadyForTagRemoval.Contains(soldierStatus.id))
                {
                    ecb.RemoveComponent<WaitForSoldiers>(0, entity);
                }
            }
        }
    }
}