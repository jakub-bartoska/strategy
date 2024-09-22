using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.battle.config;
using system.battle.battalion.execution;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(M1_MoveNotBlockedBattalions))]
    public partial struct M3_Move : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugConfig>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var debugConfig = SystemAPI.GetSingleton<DebugConfig>();
            var movingBattalions = movementDataHolder.ValueRO.movingBattalions;
            var battalionExactDistance = movementDataHolder.ValueRO.battalionExactDistance;

            new MoveBattalionJob
                {
                    debugConfig = debugConfig,
                    deltaTime = deltaTime,
                    movingBattalions = movingBattalions,
                    battalionExactDistance = battalionExactDistance
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(BattalionMarker))]
        public partial struct MoveBattalionJob : IJobEntity
        {
            public DebugConfig debugConfig;
            public float deltaTime;
            public NativeHashMap<long, Direction> movingBattalions;
            public NativeHashMap<long, float> battalionExactDistance;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform)
            {
                if (movingBattalions.TryGetValue(battalionMarker.id, out var direction))
                {
                    var finalSpeed = debugConfig.speed * deltaTime;
                    if (battalionExactDistance.TryGetValue(battalionMarker.id, out var distance))
                    {
                        finalSpeed = distance;
                    }

                    var directionCoefficient = direction switch
                    {
                        Direction.LEFT => -1,
                        Direction.RIGHT => 1,
                        Direction.NONE => 0,
                        Direction.UP => 0,
                        Direction.DOWN => 0,
                        _ => throw new Exception("Unknown direction")
                    };

                    var delta = new float3(directionCoefficient * finalSpeed, 0, 0);
                    transform.Position += delta;
                }
            }
        }
    }
}