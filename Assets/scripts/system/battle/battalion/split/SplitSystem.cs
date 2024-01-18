using System;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.battalion.fight;
using system.battle.enums;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.split
{
    [UpdateAfter(typeof(AddInFightTagSystem))]
    public partial struct SplitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var splitCandidates = SystemAPI.GetSingletonBuffer<SplitCandidate>();
            var splitCandidatesMap = new NativeHashMap<long, SplitCandidate>(splitCandidates.Length, Allocator.TempJob);
            foreach (var splitCandidate in splitCandidates)
            {
                splitCandidatesMap.Add(splitCandidate.battalionId, splitCandidate);
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            new PerformSplitJob
                {
                    splitCandidates = splitCandidatesMap,
                    ecb = ecb.AsParallelWriter(),
                    prefabHolder = prefabHolder
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct PerformSplitJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<long, SplitCandidate> splitCandidates;
            [ReadOnly] public PrefabHolder prefabHolder;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(BattalionMarker battalionMarker,
                ref BattalionHealth health,
                ref DynamicBuffer<BattalionSoldiers> soldiers,
                Entity entity,
                PossibleSplit possibleSplit,
                LocalTransform localTransform)
            {
                if (splitCandidates.TryGetValue(battalionMarker.id, out var splitCandidate))
                {
                    switch (splitCandidate.direction)
                    {
                        case Direction.UP:
                            if (!possibleSplit.up) return;
                            break;
                        case Direction.DOWN:
                            if (!possibleSplit.down) return;
                            break;
                        case Direction.LEFT:
                            if (!possibleSplit.left) return;
                            break;
                        case Direction.RIGHT:
                            if (!possibleSplit.right) return;
                            break;
                        default:
                            throw new Exception("Unknown direction");
                    }

                    var howManySoldiersShouldStay = 0;

                    switch (splitCandidate.type)
                    {
                        case SplitType.ALL:
                            howManySoldiersShouldStay = 0;
                            if (soldiers.Length < 1) return;
                            break;
                        case SplitType.MINUS_TWO:
                            howManySoldiersShouldStay = 2;
                            if (soldiers.Length < 3) return;
                            break;
                        default:
                            throw new NotImplementedException("Unknown split type " + splitCandidate.type);
                    }

                    var x = splitCandidate.direction switch
                    {
                        Direction.LEFT => localTransform.Position.x - 3.5f,
                        Direction.RIGHT => localTransform.Position.x + 3.5f,
                        _ => throw new NotImplementedException()
                    };
                    var newPosition = new float3(x, localTransform.Position.y, localTransform.Position.z);

                    var soldiersToMove = new NativeList<BattalionSoldiers>(10, Allocator.TempJob);
                    for (var i = soldiers.Length - 1; i > howManySoldiersShouldStay - 1; i--)
                    {
                        soldiersToMove.Add(soldiers[i]);
                        soldiers.RemoveAt(i);
                    }

                    //todo fix batttalionId
                    BattalionSpawner.spawnBattalionParallel(ecb, prefabHolder, 999, newPosition, battalionMarker.team, battalionMarker.row, soldiersToMove);
                }
            }
        }
    }
}