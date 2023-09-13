using System;
using component;
using component.general;
using component.pathfinding;
using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Entities;
using Unity.Mathematics;

namespace system.behaviors.debug
{
    public readonly partial struct TestAspect : IAspect
    {
        private readonly RefRO<SoldierStatus> status;
        private readonly RefRO<BehaviorContext> context;
        private readonly RefRW<MaterialColorComponent> material;
        private readonly RefRO<AgentBody> agentBody;
        private readonly RefRO<PathTracker> pathTracker;

        public void execute()
        {
            if (status.ValueRO.team == Team.TEAM2)
            {
                return;
            }

            switch (context.ValueRO.currentBehavior)
            {
                case BehaviorType.FOLLOW_CLOSEST_ENEMY:
                    if (!pathTracker.ValueRO.isMoving)
                    {
                        //material.ValueRW.Value = new float4(0, 0, 0.0f, 1);
                        material.ValueRW.Value = new float4(0f, 1f, 0f, 1);
                    }
                    else
                    {
                        material.ValueRW.Value = new float4(0, 0, 0.2f, 1);
                    }

                    break;
                case BehaviorType.SHOOT_ARROW:
                    material.ValueRW.Value = new float4(0, 0, 0.9f, 1);
                    break;
                case BehaviorType.FIGHT:
                    material.ValueRW.Value = new float4(0.6f, 0.6f, 0.6f, 1);
                    break;
                case BehaviorType.IDLE:
                    material.ValueRW.Value = new float4(0, 0, 0, 1);
                    break;
                case BehaviorType.NONE:
                    break;
                case BehaviorType.MAKE_LINE_FORMATION:
                    break;
                case BehaviorType.PROCESS_FORMATION_COMMAND:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown enum for debug");
            }
        }
    }
}