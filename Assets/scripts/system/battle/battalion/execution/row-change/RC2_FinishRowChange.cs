using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.row_change;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.row_change
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(RC1_GetRowChangeDirections))]
    public partial struct RC2_FinishRowChange : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new FinishRowSwitchJob
                {
                    ecb = ecb
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(ChangeRow))]
        public partial struct FinishRowSwitchJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            private void Execute(ref LocalTransform localTransform, Entity entity, Row row, ChangeRow changeRow)
            {
                var targetZ = CustomTransformUtils.getBattalionZPosition(row.value);
                var distanceToTarget = math.abs(localTransform.Position.z - targetZ);
                if (distanceToTarget < 0.02f)
                {
                    localTransform.Position.z = targetZ;
                    //ecb.DestroyEntity(changeRow.shadowEntity);
                    ecb.RemoveComponent<ChangeRow>(entity);
                }
            }
        }
    }
}