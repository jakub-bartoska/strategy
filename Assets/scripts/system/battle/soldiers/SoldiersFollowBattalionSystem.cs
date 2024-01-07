using component;
using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.soldiers
{
    public partial struct SoldiersFollowBattalionSystem : ISystem
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
            var battalionPositions = new NativeParallelHashMap<long, float3>(1000, Allocator.TempJob);
            var soldierToBattalionMap = new NativeParallelHashMap<long, long>(10000, Allocator.TempJob);

            new CollectBattalionPositionsJob
                {
                    battalionPositions = battalionPositions.AsParallelWriter(),
                    soldierToBattalionMap = soldierToBattalionMap.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            new SoldierMovementSystem
                {
                    battalionPositions = battalionPositions,
                    soldierToBattalionMap = soldierToBattalionMap
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectBattalionPositionsJob : IJobEntity
        {
            public NativeParallelHashMap<long, float3>.ParallelWriter battalionPositions;
            public NativeParallelHashMap<long, long>.ParallelWriter soldierToBattalionMap;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers, LocalTransform localTransform)
            {
                battalionPositions.TryAdd(battalionMarker.id, localTransform.Position);
                foreach (var soldier in soldiers)
                {
                    soldierToBattalionMap.TryAdd(soldier.soldierId, battalionMarker.id);
                }
            }
        }

        [BurstCompile]
        public partial struct SoldierMovementSystem : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<long, float3> battalionPositions;
            [ReadOnly] public NativeParallelHashMap<long, long> soldierToBattalionMap;

            private void Execute(SoldierStatus soldierStatus, ref LocalTransform localTransform)
            {
                if (soldierToBattalionMap.TryGetValue(soldierStatus.index, out var battalionId))
                {
                    if (battalionPositions.TryGetValue(battalionId, out var battalionPosition))
                    {
                        localTransform.Position.x = battalionPosition.x;
                    }
                }
            }
        }
    }
}