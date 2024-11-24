using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.battle.battalion.markers;
using component.battle.config;
using system.battle.enums;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.row_change
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(RC2_FinishRowChange))]
    public partial struct RC3_MoveBetweenRows : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<DebugConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var speed = SystemAPI.GetSingleton<DebugConfig>().speed;
            var battalionsPerformingAction = dataHolder.ValueRO.battalionsPerformingAction;

            new MoveToNewLineJob
                {
                    deltaTime = deltaTime,
                    speed = speed,
                    battalionsPerformingAction = battalionsPerformingAction
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct MoveToNewLineJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float speed;
            public NativeHashSet<long> battalionsPerformingAction;

            private void Execute(BattalionMarker battalionMarker, ChangeRow changeRow,
                ref LocalTransform localTransform, Row row)
            {
                battalionsPerformingAction.Add(battalionMarker.id);

                var targetZ = CustomTransformUtils.getBattalionZPosition(row.value, 10);
                var travelDistance = deltaTime * speed * 0.5f;
                var distanceToTarget = math.abs(localTransform.Position.z - targetZ);
                if (distanceToTarget <= travelDistance)
                {
                    localTransform.Position.z = targetZ;
                    return;
                }

                var resultZ = changeRow.direction switch
                {
                    Direction.UP => localTransform.Position.z + travelDistance,
                    Direction.DOWN => localTransform.Position.z - travelDistance,
                    _ => throw new NotImplementedException()
                };
                localTransform.Position.z = resultZ;
            }
        }
    }
}