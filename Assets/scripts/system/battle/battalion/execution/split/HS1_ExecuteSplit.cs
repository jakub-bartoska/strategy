using System;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
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
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var splitBattalions = dataHolder.ValueRO.splitBattalions;
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
        [ReadOnly] public NativeHashMap<long, SplitInfo> splitBattalions;
        [ReadOnly] public PrefabHolder prefabHolder;
        public EntityCommandBuffer ecb;
        [NativeDisableUnsafePtrRestriction] public RefRW<BattalionIdHolder> battalionIdHolder;

        private void Execute(BattalionMarker battalionMarker,
            ref DynamicBuffer<BattalionSoldiers> soldiers,
            BattalionTeam team,
            Row row,
            ref BattalionHealth health,
            LocalTransform localTransform,
            BattalionWidth width)
        {
            if (!splitBattalions.ContainsKey(battalionMarker.id))
            {
                return;
            }

            var splitDirection = splitBattalions[battalionMarker.id];

            var soldierCountToStay = splitDirection.verticalFightType switch
            {
                VerticalFightType.UP => 2,
                VerticalFightType.DOWN => 2,
                VerticalFightType.BOTH => 4,
                _ => throw new Exception("Unknown vertical fight type")
            };

            if (soldiers.Length <= soldierCountToStay)
            {
                return;
            }

            var positionsToStay = getSoldiersPositionsToStay(soldiers, splitDirection.verticalFightType);

            var soldiersToMove = new NativeList<BattalionSoldiers>(10, Allocator.Temp);
            var soldiersToStay = new NativeList<BattalionSoldiers>(10, Allocator.Temp);
            foreach (var soldier in soldiers)
            {
                if (!positionsToStay.Contains(soldier.positionWithinBattalion))
                {
                    soldiersToMove.Add(soldier);
                    health.value -= 10;
                }
                else
                {
                    soldiersToStay.Add(soldier);
                }
            }

            soldiers.Clear();
            soldiers.AddRange(soldiersToStay);

            var newPosition = BattleTransformUtils.getNewPositionForSplit(localTransform.Position, width.value, splitDirection.movamentDirrection);
            BattalionSpawner.spawnBattalionParallel(ecb, prefabHolder, battalionIdHolder.ValueRW.nextBattalionId++, newPosition, team.value, row.value, soldiersToMove, battalionMarker.soldierType);
        }

        private NativeHashSet<int> getSoldiersPositionsToStay(DynamicBuffer<BattalionSoldiers> battalionSoldiers, VerticalFightType fightType)
        {
            var sortedBattalionIds = new NativeList<int>(10, Allocator.Temp);
            foreach (var battalionSoldier in battalionSoldiers)
            {
                sortedBattalionIds.Add(battalionSoldier.positionWithinBattalion);
            }

            sortedBattalionIds.Sort();

            var result = new NativeHashSet<int>(10, Allocator.Temp);
            switch (fightType)
            {
                case VerticalFightType.UP:
                    result.Add(sortedBattalionIds[^1]);
                    result.Add(sortedBattalionIds[^2]);
                    break;
                case VerticalFightType.DOWN:
                    result.Add(sortedBattalionIds[0]);
                    result.Add(sortedBattalionIds[1]);
                    break;
                case VerticalFightType.BOTH:
                    result.Add(sortedBattalionIds[0]);
                    result.Add(sortedBattalionIds[1]);
                    result.Add(sortedBattalionIds[^1]);
                    result.Add(sortedBattalionIds[^2]);
                    break;
                default:
                    throw new Exception("Unknown vertical fight type");
            }

            return result;
        }
    }
}