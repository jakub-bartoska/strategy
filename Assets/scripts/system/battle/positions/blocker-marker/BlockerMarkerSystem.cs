using System;
using System.Linq;
using component;
using component._common.system_switchers;
using component.general;
using component.helpers.positioning;
using component.pathfinding;
using component.soldier;
using component.soldier.behavior.behaviors;
using system.positions.path_tracker;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.positions.blocker_marker
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PathTrackingSystem))]
    public partial struct BlockerMarkerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PositionHolder>();
            state.RequireForUpdate<PositionHolderConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattleSoldierCounts>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var battleSoldierCounts = SystemAPI.GetSingleton<BattleSoldierCounts>();
            var totalSoldiers = battleSoldierCounts.team1Count + battleSoldierCounts.team2Count;
            var positionHolder = SystemAPI.GetSingleton<PositionHolder>();
            var positionHolderConfig = SystemAPI.GetSingleton<PositionHolderConfig>();

            var blockers = new NativeParallelMultiHashMap<int2, int>(totalSoldiers * 2, Allocator.TempJob);
            var blocked = new NativeParallelMultiHashMap<int2, int>(totalSoldiers * 2, Allocator.TempJob);
            new BlockerMarkerSortJob
                {
                    blockers = blockers.AsParallelWriter(),
                    blocked = blocked.AsParallelWriter(),
                    positionHolderConfig = positionHolderConfig
                }.ScheduleParallel(state.Dependency)
                .Complete();

            if (blocked.Count() == 0 || blockers.Count() == 0)
            {
                return;
            }

            var blockingCellIds = getUniqueKeys(blocked);
            var soldierPositions = positionHolder.soldierIdPosition;
            var result = new NativeParallelHashSet<int>(totalSoldiers, Allocator.TempJob);

            new BlockerMarkerJob
                {
                    blockingCellIds = blockingCellIds,
                    blockers = blockers,
                    blocked = blocked,
                    soldierIdPositions = soldierPositions,
                    result = result.AsParallelWriter()
                }.Schedule(blockingCellIds.Count(), 5)
                .Complete();
        }

        private NativeArray<int2> getUniqueKeys(NativeParallelMultiHashMap<int2, int> map)
        {
            var keys = map.GetKeyArray(Allocator.TempJob);
            var uniqueCount = keys.Unique();
            return keys.GetSubArray(0, uniqueCount);
        }
    }

    [BurstCompile]
    public partial struct BlockerMarkerSortJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, int>.ParallelWriter blockers;
        public NativeParallelMultiHashMap<int2, int>.ParallelWriter blocked;
        [NativeDisableUnsafePtrRestriction] public PositionHolderConfig positionHolderConfig;

        [BurstCompile]
        private void Execute(SoldierStatus status,
            LocalTransform transform,
            BehaviorContext behavior,
            PathTracker pathTracker)
        {
            if (status.team == Team.TEAM2)
            {
                return;
            }

            var currentBehavior = behavior.currentBehavior;
            switch (currentBehavior)
            {
                case BehaviorType.FIGHT:
                    break;
                case BehaviorType.IDLE:
                case BehaviorType.NONE:
                case BehaviorType.MAKE_LINE_FORMATION:
                case BehaviorType.PROCESS_FORMATION_COMMAND:
                case BehaviorType.SHOOT_ARROW:
                    addKey(transform.Position.xz, status.index, blockers, false);
                    break;
                case BehaviorType.FOLLOW_CLOSEST_ENEMY:
                    if (!pathTracker.isMoving)
                    {
                        addKey(transform.Position.xz, status.index, blocked, true);
                    }

                    break;
                default:
                    throw new Exception("unknown enum");
            }
        }

        private void addKey(float2 position, int id, NativeParallelMultiHashMap<int2, int>.ParallelWriter map,
            bool isBlocked)
        {
            var xOffset = positionHolderConfig.minSquarePosition.x;
            var localXPlusOffset = position.x - xOffset;
            var xColumn = (int) (localXPlusOffset / positionHolderConfig.oneSquareSize);

            var yOffset = positionHolderConfig.minSquarePosition.y;
            var localYPlusOffset = position.y - yOffset;
            var yColumn = (int) (localYPlusOffset / positionHolderConfig.oneSquareSize);

            map.Add(new int2(xColumn, yColumn), id);

            if (isBlocked)
            {
                return;
            }

            if (localXPlusOffset % positionHolderConfig.oneSquareSize < 1)
            {
                map.Add(new int2(xColumn - 1, yColumn), id);
            }

            if (localXPlusOffset % positionHolderConfig.oneSquareSize > positionHolderConfig.oneSquareSize - 1)
            {
                map.Add(new int2(xColumn + 1, yColumn), id);
            }

            if (localYPlusOffset % positionHolderConfig.oneSquareSize < 1)
            {
                map.Add(new int2(xColumn, yColumn - 1), id);
            }

            if (localYPlusOffset % positionHolderConfig.oneSquareSize > positionHolderConfig.oneSquareSize - 1)
            {
                map.Add(new int2(xColumn, yColumn + 1), id);
            }
        }
    }

    [BurstCompile]
    public partial struct BlockerMarkerJob : IJobParallelFor
    {
        public NativeArray<int2> blockingCellIds;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int> blockers;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int> blocked;
        [ReadOnly] public NativeParallelMultiHashMap<int, float3> soldierIdPositions;
        public NativeParallelHashSet<int>.ParallelWriter result;

        public void Execute(int index)
        {
            var blockingCell = blockingCellIds[index];
            foreach (var blockedId in blocked.GetValuesForKey(blockingCell))
            {
                if (soldierIdPositions.TryGetFirstValue(blockedId, out var blockedPosition, out _))
                {
                    foreach (var blockerId in blockers.GetValuesForKey(blockingCell))
                    {
                        if (soldierIdPositions.TryGetFirstValue(blockerId, out var blockerPosition, out _))
                        {
                            if (math.distance(blockedPosition.xz, blockerPosition.xz) < 1.5)
                            {
                                result.Add(blockerId);
                            }
                        }
                    }
                }
            }
        }
    }
}