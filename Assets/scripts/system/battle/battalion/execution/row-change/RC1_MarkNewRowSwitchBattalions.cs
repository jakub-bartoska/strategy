using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.row_change
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public partial struct RC1_MarkNewROwSwitchBattalions : ISystem
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
            var battalionSwitchRowDirections = DataHolder.battalionSwitchRowDirections;
            var battalionsPerformingAction = DataHolder.battalionsPerformingAction;

            //todo filter out not moving battalions
            new MarkRowSwitchJob
                {
                    battalionSwitchRowDirections = battalionSwitchRowDirections,
                    ecb = ecb,
                    battalionsPerformingAction = battalionsPerformingAction
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithNone(typeof(ChangeRow))]
        public partial struct MarkRowSwitchJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public NativeHashMap<long, Direction> battalionSwitchRowDirections;
            public NativeHashSet<long> battalionsPerformingAction;

            private void Execute(BattalionMarker battalionMarker, Entity entity, ref Row row)
            {
                if (battalionsPerformingAction.Contains(battalionMarker.id))
                {
                    return;
                }

                if (battalionSwitchRowDirections.TryGetValue(battalionMarker.id, out var direction) && direction != Direction.NONE)
                {
                    ecb.AddComponent(entity, new ChangeRow
                    {
                        direction = direction,
                    });
                    var newRow = direction switch
                    {
                        Direction.UP => row.value - 1,
                        Direction.DOWN => row.value + 1,
                        _ => throw new Exception("unknown direction")
                    };
                    row.value = newRow;
                }
            }
        }
    }
}