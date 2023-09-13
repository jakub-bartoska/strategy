using component._common.movement_agents;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.general;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.movement
{
    public partial struct FollowArmySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<AgentMovementAllowedTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var armyPositions = new NativeParallelHashMap<long, float3>(1000, Allocator.TempJob);
            new UpdateArmyPositionsJob
                {
                    armyPositions = armyPositions.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            new UpdateArmyFollowsJob
                {
                    ecb = ecb.AsParallelWriter(),
                    armyPositions = armyPositions
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    public partial struct UpdateArmyPositionsJob : IJobEntity
    {
        public NativeParallelHashMap<long, float3>.ParallelWriter armyPositions;

        private void Execute(ArmyTag tag, LocalTransform transform, IdHolder idHolder)
        {
            armyPositions.TryAdd(idHolder.id, transform.Position);
        }
    }

    public partial struct UpdateArmyFollowsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<long, float3> armyPositions;
        public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute(ArmyTag tag, ref AgentBody agentBody, ref ArmyMovementStatus movementStatus, Entity entity,
            IdHolder idHolder)
        {
            if (movementStatus.movementType != MovementType.FOLLOW_ARMY)
            {
                return;
            }

            if (armyPositions.TryGetValue(movementStatus.targetArmyId.Value, out var targetPosition))
            {
                agentBody.IsStopped = false;
                agentBody.Destination = targetPosition;
            }
            else
            {
                var newMovement = new ArmyMovementStatus
                {
                    movementType = MovementType.IDLE
                };
                ecb.SetComponent((int) idHolder.id, entity, newMovement);
            }
        }
    }
}