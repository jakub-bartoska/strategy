﻿using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    public partial struct BattalionDirectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var battalionDefaultMovementDirection = DataHolder.battalionDefaultMovementDirection;
            new CollectBattalionDirections
                {
                    battalionDirections = battalionDefaultMovementDirection
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct CollectBattalionDirections : IJobEntity
        {
            public NativeHashMap<long, Direction> battalionDirections;

            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection)
            {
                battalionDirections.Add(battalionMarker.id, movementDirection.plannedDirection);
            }
        }
    }
}