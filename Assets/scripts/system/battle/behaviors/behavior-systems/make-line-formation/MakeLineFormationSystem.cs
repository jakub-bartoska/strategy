using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.formation;
using component.general;
using component.soldier;
using component.soldier.behavior.behaviors;
using system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[assembly:
    RegisterGenericJobType(
        typeof(Unity.Collections.SortJob<int,
            system.behaviors.behavior_systems.make_line_formation.FormationPositionComparator>))]

namespace system.behaviors.behavior_systems.make_line_formation
{
    //[BurstCompile]
    //[DeallocateOnJobCompletion]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct MakeLineFormationSystem : ISystem
    {
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattleSoldierCounts>();
        }

        //[BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var battleSoldierCounts = SystemAPI.GetSingleton<BattleSoldierCounts>();
            var totalSoldiers = battleSoldierCounts.team1Count + battleSoldierCounts.team2Count;
            var soldiersForFormation = new NativeParallelMultiHashMap<int, float3>(totalSoldiers, Allocator.TempJob);
            state.Dependency = new FindLineEntitiesJob
            {
                resultMap = soldiersForFormation.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();

            var soldierCount = soldiersForFormation.Count();

            if (soldierCount == 0)
            {
                return;
            }

            var ecb =
                SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged);

            var formationManager = SystemAPI.GetSingletonRW<FormationManager>();
            formationManager.ValueRW.maxFormationId += 1;


            var keyArray = soldiersForFormation.GetKeyArray(Allocator.TempJob);

            var map = multiHashMapToHashMap(keyArray, soldiersForFormation);
            keyArray.SortJob(new FormationPositionComparator(map))
                .Schedule()
                .Complete();

            var result = new NativeHashMap<int, int>(keyArray.Length, Allocator.TempJob);
            for (var i = 0; i < keyArray.Length; i++)
            {
                result.Add(keyArray[i], i);
            }

            new UpdateSoldierFormationStatus
                {
                    fomrationSoldiers = keyArray,
                    formationId = formationManager.ValueRO.maxFormationId
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var middle = new float3();
            var allPositions = map.GetValueArray(Allocator.TempJob);
            foreach (var position in allPositions)
            {
                middle += position;
            }

            middle /= allPositions.Length;

            var formationEntity = ecb.CreateEntity();
            var formation = new FormationContext
            {
                id = formationManager.ValueRO.maxFormationId,
                formationType = FormationType.LINE,
                formationSize = soldierCount,
                distanceBetweenSoldiers = 1.5f,
                soldierIdToFormationIndex = result,
                formationCenter = middle
            };
            var deleteOnBattleFinish = new BattleCleanupTag();

            ecb.AddComponent(formationEntity, formation);
            ecb.AddComponent(formationEntity, deleteOnBattleFinish);
        }

        private NativeHashMap<int, float3> multiHashMapToHashMap(
            NativeArray<int> keys,
            NativeParallelMultiHashMap<int, float3> input
        )
        {
            var result = new NativeHashMap<int, float3>(keys.Length, Allocator.TempJob);
            foreach (var key in keys)
            {
                input.TryGetFirstValue(key, out var value, out _);
                result.Add(key, value);
            }

            return result;
        }
    }

    [BurstCompile]
    public readonly struct FormationPositionComparator : IComparer<int>
    {
        [ReadOnly] public readonly NativeHashMap<int, float3> idPositionmap;

        public FormationPositionComparator(NativeHashMap<int, float3> idPositionmap)
        {
            this.idPositionmap = idPositionmap;
        }

        public int Compare(int x, int y)
        {
            var xPosition = idPositionmap[x];
            var yPosition = idPositionmap[y];

            var res = yPosition.z.CompareTo(xPosition.z);
            return res;
        }
    }

    [BurstCompile]
    public partial struct FindLineEntitiesJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, float3>.ParallelWriter resultMap;

        private void Execute(BehaviorContext behaviorContext, LocalTransform localTransform, SoldierStatus status,
            SoldierFormationStatus formationStatus)
        {
            if (formationStatus.formationStatus == FormationStatus.IN_FORMATION)
            {
                return;
            }

            if (behaviorContext.currentBehavior == BehaviorType.MAKE_LINE_FORMATION)
            {
                resultMap.Add(status.index, localTransform.Position);
            }
        }
    }

    [BurstCompile]
    public partial struct UpdateSoldierFormationStatus : IJobEntity
    {
        public int formationId;
        public NativeArray<int> fomrationSoldiers;

        private void Execute(SoldierStatus status, ref SoldierFormationStatus formationStatus)
        {
            if (formationStatus.formationStatus == FormationStatus.IN_FORMATION)
            {
                return;
            }

            if (fomrationSoldiers.Contains(status.index))
            {
                formationStatus.formationId = formationId;
                formationStatus.formationStatus = FormationStatus.IN_FORMATION;
            }
        }
    }
}