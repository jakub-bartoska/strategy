using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.utils;
using system.battle.enums;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace system.battle.battalion.split
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public partial struct HS1_ExecuteSplit : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var splitBattalions = DataHolder.splitBattalions;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var battalionIdHolder = SystemAPI.GetSingletonRW<BattalionIdHolder>();

            new HorizontalSplitJob
                {
                    splitBattalions = splitBattalions,
                    ecb = ecb,
                    prefabHolder = prefabHolder,
                    battalionIdHolder = battalionIdHolder
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct HorizontalSplitJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Direction> splitBattalions;
        [ReadOnly] public PrefabHolder prefabHolder;
        public EntityCommandBuffer ecb;
        [NativeDisableUnsafePtrRestriction] public RefRW<BattalionIdHolder> battalionIdHolder;

        private void Execute(BattalionMarker battalionMarker,
            ref DynamicBuffer<BattalionSoldiers> soldiers,
            BattalionTeam team, Row row,
            ref BattalionHealth health,
            LocalTransform localTransform,
            BattalionWidth width)
        {
            if (!splitBattalions.ContainsKey(battalionMarker.id))
            {
                return;
            }

            var soldierCountToStay = 2;

            //min soldier count is 2
            if (soldiers.Length <= soldierCountToStay)
            {
                return;
            }

            var newPosition = BattleTransformUtils.getNewPositionForSplit(localTransform.Position, width.value, splitBattalions[battalionMarker.id]);

            var soldiersToMove = new NativeList<BattalionSoldiers>(10, Allocator.TempJob);
            for (var i = soldiers.Length - 1; i > soldierCountToStay - 1; i--)
            {
                soldiersToMove.Add(soldiers[i]);
                soldiers.RemoveAt(i);
            }

            BattalionSpawner.spawnBattalionParallel(ecb, prefabHolder, battalionIdHolder.ValueRW.nextBattalionId++, newPosition, team.value, row.value, soldiersToMove, battalionMarker.soldierType);
        }
    }
}