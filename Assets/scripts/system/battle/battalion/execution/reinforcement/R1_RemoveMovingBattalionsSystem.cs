﻿using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.reinforcement
{
    /**
     * Moving battalions are not able to receive reinforcements
     */
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(BattalionBehaviorPickerSystem))]
    public partial struct R1_RemoveMovingBattalionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new UpdateMovementDirectionJob()
                .Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct UpdateMovementDirectionJob : IJobEntity
        {
            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection)
            {
                if (movementDirection.currentDirection != Direction.NONE)
                {
                    DataHolder.needReinforcements.Remove(battalionMarker.id);
                }
            }
        }
    }
}