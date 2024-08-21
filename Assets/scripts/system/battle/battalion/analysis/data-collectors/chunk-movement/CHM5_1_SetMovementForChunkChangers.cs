using System;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM5_0_SetMovement))]
    public partial struct CHM5_1_SetMovementForChunkChangers : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var plannedMovementDirections = movementDataHolder.ValueRW.plannedMovementDirections;

            var backupPlanDataHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();

            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var battalionSwitchRowDirections = dataHolder.ValueRO.battalionSwitchRowDirections;
            var allBattalions = dataHolder.ValueRO.battalionInfo;

            foreach (var battalionId in backupPlanDataHolder.ValueRO.moveToDifferentChunk)
            {
                var resultDirection = getBattalionDirection(
                    battalionId,
                    backupPlanDataHolder.ValueRO.battalionIdToChunk,
                    backupPlanDataHolder.ValueRO.chunkReinforcementPaths,
                    backupPlanDataHolder.ValueRO.allChunks,
                    allBattalions
                );
                switch (resultDirection)
                {
                    case Direction.LEFT:
                    case Direction.RIGHT:
                        plannedMovementDirections.Add(battalionId, resultDirection);
                        break;
                    case Direction.UP:
                    case Direction.DOWN:
                        battalionSwitchRowDirections.Add(battalionId, resultDirection);
                        break;
                    case Direction.NONE:
                        plannedMovementDirections.Add(battalionId, resultDirection);
                        break;
                    default:
                        throw new Exception("Invalid direction " + resultDirection + " for battalion " + battalionId);
                }
            }
        }

        private Direction getBattalionDirection(
            long battalionId,
            NativeHashMap<long, long> battalionIdToChunk,
            NativeHashMap<long, ChunkPath> chunkReinforcementPaths,
            NativeHashMap<long, BattleChunk> allChunks,
            NativeHashMap<long, BattalionInfo> allBattalions
        )
        {
            var myChunkId = battalionIdToChunk[battalionId];
            var reinforcementPath = chunkReinforcementPaths[myChunkId];
            switch (reinforcementPath.pathType)
            {
                case PathType.NO_VALID_PATH:
                    return Direction.NONE;
                case PathType.PATH:
                case PathType.TARGET:
                    break;
                default:
                    throw new Exception("Unknown path type.");
            }

            var myBattalion = allBattalions[battalionId];
            var targetChunk = allChunks[reinforcementPath.targetChunkId];
            var xDirection = getXDirection(myBattalion, targetChunk);
            var switchLines = canSwitchLines(myBattalion, targetChunk, xDirection);
            if (switchLines)
            {
                var myChunk = allChunks[myChunkId];
                return getRowDirections(myChunk, targetChunk);
            }

            return xDirection;
        }

        private Direction getXDirection(BattalionInfo myBattalion, BattleChunk targetChunk)
        {
            var targetMiddle = (targetChunk.startX + targetChunk.endX) / 2;

            if (myBattalion.position.x > targetMiddle)
            {
                return Direction.LEFT;
            }

            return Direction.RIGHT;
        }

        private bool canSwitchLines(BattalionInfo myBattalion, BattleChunk targetChunk, Direction xDirection)
        {
            switch (xDirection)
            {
                case Direction.LEFT:
                    if (myBattalion.position.x < targetChunk.endX - myBattalion.width)
                    {
                        return true;
                    }

                    return false;
                case Direction.RIGHT:
                    if (myBattalion.position.x > targetChunk.startX + myBattalion.width)
                    {
                        return true;
                    }

                    return false;
                default:
                    throw new Exception("Invalid direction");
            }
        }

        private Direction getRowDirections(BattleChunk myChunk, BattleChunk targetChunk)
        {
            if (myChunk.rowId < targetChunk.rowId)
            {
                return Direction.DOWN;
            }

            return Direction.UP;
        }
    }
}