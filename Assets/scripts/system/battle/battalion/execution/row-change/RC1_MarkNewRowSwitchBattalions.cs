using System;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.battle.battalion.markers;
using system.battle.enums;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace system.battle.battalion.row_change
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public partial struct RC1_MarkNewRowSwitchBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var battalionSwitchRowDirections = dataHolder.ValueRO.battalionSwitchRowDirections;
            var battalionsPerformingAction = dataHolder.ValueRO.battalionsPerformingAction;
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            new MarkRowSwitchJob
                {
                    battalionSwitchRowDirections = battalionSwitchRowDirections,
                    ecb = ecb,
                    battalionsPerformingAction = battalionsPerformingAction,
                    prefabHolder = prefabHolder
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
            public PrefabHolder prefabHolder;

            private void Execute(BattalionMarker battalionMarker, Entity entity, ref Row row, LocalTransform transform,
                BattalionTeam team, BattalionWidth width)
            {
                if (battalionsPerformingAction.Contains(battalionMarker.id))
                {
                    return;
                }

                if (battalionSwitchRowDirections.TryGetValue(battalionMarker.id, out var direction) &&
                    direction != Direction.NONE)
                {
                    var shadowEntity = BattalionShadowSpawner.spawnBattalionShadow(ecb, prefabHolder,
                        transform.Position, battalionMarker.id, row.value, team.value, width.value);
                    ecb.AddComponent(entity, new ChangeRow
                    {
                        direction = direction,
                        shadowEntity = shadowEntity
                    });
                    var newRow = direction switch
                    {
                        Direction.UP => row.value - 1,
                        Direction.DOWN => row.value + 1,
                        _ => throw new Exception("unknown direction")
                    };
                    row.value = newRow;
                    battalionsPerformingAction.Add(battalionMarker.id);
                }
            }
        }
    }
}