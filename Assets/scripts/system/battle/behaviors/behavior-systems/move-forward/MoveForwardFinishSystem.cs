using component._common.system_switchers;
using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.behaviors.behavior_systems.move_forward
{
    public partial struct MoveForwardFinishSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new MoveForwardFinishJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct MoveForwardFinishJob : IJobEntity
        {
            private void Execute(ref BehaviorContext behaviorContext, ref AgentBody agentBody)
            {
                if (behaviorContext.behaviorToBeFinished != BehaviorType.MOVE_FORWARD)
                {
                    return;
                }

                agentBody.IsStopped = true;
                behaviorContext.behaviorToBeFinished = BehaviorType.NONE;
            }
        }
    }
}