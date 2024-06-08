using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.config;
using system.battle.battalion;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.soldiers
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(M3_Move))]
    public partial struct M4_SoldiersFollowBattalionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var speed = SystemAPI.GetSingleton<DebugConfig>().speed;
            var battalionPositions = new NativeParallelHashMap<long, float3>(1000, Allocator.TempJob); //ok
            var soldierToBattalionMap = new NativeParallelHashMap<long, (long, BattalionSoldiers)>(10000, Allocator.TempJob); //ok
            var deltaTime = SystemAPI.Time.DeltaTime;

            new CollectBattalionPositionsJob
                {
                    battalionPositions = battalionPositions.AsParallelWriter(),
                    soldierToBattalionMap = soldierToBattalionMap.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            new SoldierMovementSystem
                {
                    deltaTime = deltaTime,
                    battalionPositions = battalionPositions,
                    soldierToBattalionMap = soldierToBattalionMap,
                    speed = speed
                }.ScheduleParallel(state.Dependency)
                .Complete();
            battalionPositions.Dispose();
            soldierToBattalionMap.Dispose();
        }

        [BurstCompile]
        public partial struct CollectBattalionPositionsJob : IJobEntity
        {
            public NativeParallelHashMap<long, float3>.ParallelWriter battalionPositions;
            public NativeParallelHashMap<long, (long, BattalionSoldiers)>.ParallelWriter soldierToBattalionMap;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers, LocalTransform localTransform)
            {
                battalionPositions.TryAdd(battalionMarker.id, localTransform.Position);
                foreach (var soldier in soldiers)
                {
                    soldierToBattalionMap.TryAdd(soldier.soldierId, (battalionMarker.id, soldier));
                }
            }
        }

        [BurstCompile]
        public partial struct SoldierMovementSystem : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public NativeParallelHashMap<long, float3> battalionPositions;
            [ReadOnly] public NativeParallelHashMap<long, (long, BattalionSoldiers)> soldierToBattalionMap;
            [ReadOnly] public float speed;

            private void Execute(SoldierStatus soldierStatus, ref LocalTransform localTransform)
            {
                if (soldierToBattalionMap.TryGetValue(soldierStatus.index, out var value))
                {
                    if (battalionPositions.TryGetValue(value.Item1, out var battalionPosition))
                    {
                        var finalSpeed = getSpeed(battalionPosition, localTransform.Position);

                        var z = battalionPosition.z - 5 + value.Item2.positionWithinBattalion + 0.5f;
                        var positionInBattalion = new float3(battalionPosition.x, battalionPosition.y, z);
                        var direction = positionInBattalion - localTransform.Position;
                        if (math.distancesq(direction, float3.zero) < 0.05f)
                        {
                            localTransform.Position = positionInBattalion;
                        }
                        else
                        {
                            var normalizedPosition = math.normalize(direction);
                            localTransform.Position += (normalizedPosition * finalSpeed);
                        }
                    }
                }
            }

            private float getSpeed(float3 battalionPosition, float3 soldierPosition)
            {
                var normalSpeed = speed * deltaTime;
                var diagonalCoefficient = 1.41f;
                var diagonalSpeed = speed * diagonalCoefficient * deltaTime;

                var zDelta = math.abs(battalionPosition.z - soldierPosition.z);
                var xDelta = math.abs(battalionPosition.x - soldierPosition.x);
                if (zDelta > 0.1f && xDelta > 0.1f) return diagonalSpeed;

                return normalSpeed;
            }
        }
    }
}