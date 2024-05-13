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
            return;
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
        [WithAll(typeof(BattalionMarker))]
        public partial struct ManageChangeStates : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Entity entity, ref ChangeRow changeRow, ref LocalTransform localTransform, ref Row row)
            {
                switch (changeRow.state)
                {
                    case ChangeState.INIT:
                        initRowChange(ref row, ref changeRow);
                        break;
                    case ChangeState.RUNNING:
                        isFinished(row, localTransform, entity, changeRow);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            private void initRowChange(ref Row row, ref ChangeRow changeRow)
            {
                var newRow = changeRow.direction switch
                {
                    Direction.UP => row.value - 1,
                    Direction.DOWN => row.value + 1,
                    _ => throw new NotImplementedException()
                };
                row.value = newRow;
                changeRow.state = ChangeState.RUNNING;
            }

            private void isFinished(Row row, LocalTransform localTransform, Entity entity, ChangeRow changeRow)
            {
                var targetZ = CustomTransformUtils.getBattalionZPosition(row.value);
                var distanceToTarget = math.abs(localTransform.Position.z - targetZ);
                if (distanceToTarget < 0.02f)
                {
                    localTransform.Position.z = targetZ;
                    ecb.DestroyEntity(0, changeRow.shadowEntity);
                    ecb.RemoveComponent<ChangeRow>(1, entity);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(BattalionMarker))]
        public partial struct MoveToNewLineJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float speed;

            private void Execute(ChangeRow changeRow, ref LocalTransform localTransform, Row row)
            {
                if (changeRow.state != ChangeState.RUNNING) return;

                var targetZ = CustomTransformUtils.getBattalionZPosition(row.value);
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