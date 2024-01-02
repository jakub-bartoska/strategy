using System;
using component;
using component._common.movement_agents;
using component._common.system_switchers;
using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace system.battle.behaviors.behavior_systems.move_forward
{
    public partial struct MoveForwardBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentMovementAllowedForBattleTag>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new MoveForwardJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct MoveForwardJob : IJobEntity
        {
            private void Execute(BehaviorContext behaviorContext, ref AgentBody agentBody, LocalTransform transform, SoldierStatus status)
            {
                if (behaviorContext.currentBehavior != BehaviorType.MOVE_FORWARD) return;

                var targetPosition = transform.Position;
                targetPosition.x = status.team switch
                {
                    Team.TEAM1 => targetPosition.x - 1000,
                    Team.TEAM2 => targetPosition.x + 1000,
                    _ => throw new Exception("Unknown team")
                };

                agentBody.IsStopped = false;
                agentBody.Destination = targetPosition;
            }
        }
    }
}