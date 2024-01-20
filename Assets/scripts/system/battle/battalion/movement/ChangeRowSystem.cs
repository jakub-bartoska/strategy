using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.enums;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    public partial struct ChangeRowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new ManageChangeStates
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var deltaTime = SystemAPI.Time.DeltaTime;

            new MoveToNewLineJob
                {
                    deltaTime = deltaTime
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct ManageChangeStates : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(ref BattalionMarker battalionMarker, Entity entity, ref ChangeRow changeRow, ref LocalTransform localTransform)
            {
                switch (changeRow.state)
                {
                    case ChangeState.INIT:
                        initRowChange(ref battalionMarker, ref changeRow);
                        break;
                    case ChangeState.RUNNING:
                        isFinished(battalionMarker, localTransform, entity);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            private void initRowChange(ref BattalionMarker battalionMarker, ref ChangeRow changeRow)
            {
                var newRow = changeRow.direction switch
                {
                    Direction.UP => battalionMarker.row - 1,
                    Direction.DOWN => battalionMarker.row + 1,
                    _ => throw new System.NotImplementedException()
                };
                battalionMarker.row = newRow;
                changeRow.state = ChangeState.RUNNING;
            }

            private void isFinished(BattalionMarker battalionMarker, LocalTransform localTransform, Entity entity)
            {
                var targetZ = CustomTransformUtils.getBattalionZPosition(battalionMarker.row);
                var distanceToTarget = math.abs(localTransform.Position.z - targetZ);
                if (distanceToTarget < 0.02f)
                {
                    localTransform.Position.z = targetZ;
                    ecb.RemoveComponent<ChangeRow>(0, entity);
                }
            }
        }

        [BurstCompile]
        public partial struct MoveToNewLineJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;

            private void Execute(BattalionMarker battalionMarker, ChangeRow changeRow, ref LocalTransform localTransform)
            {
                if (changeRow.state != ChangeState.RUNNING) return;

                var targetZ = CustomTransformUtils.getBattalionZPosition(battalionMarker.row);
                var travelDistance = deltaTime * 5f;
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