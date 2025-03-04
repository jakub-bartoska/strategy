﻿using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    public partial struct DataCleanerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var backupPlanDataHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();

            dataHolder.ValueRW.positions.Clear();
            dataHolder.ValueRW.fightingPairs.Clear();
            dataHolder.ValueRW.battalionsPerformingAction.Clear();
            dataHolder.ValueRW.needReinforcements.Clear();
            dataHolder.ValueRW.allBattalionIds.Clear();
            dataHolder.ValueRW.reinforcements.Clear();
            dataHolder.ValueRW.rowChanges.Clear();
            dataHolder.ValueRW.battalionSwitchRowDirections.Clear();
            dataHolder.ValueRW.blockedHorizontalSplits.Clear();
            dataHolder.ValueRW.splitBattalions.Clear();
            dataHolder.ValueRW.fightingBattalions.Clear();
            dataHolder.ValueRW.battalionInfo.Clear();
            dataHolder.ValueRW.declinedReinforcements.Clear();

            movementDataHolder.ValueRW.inFightMovement.Clear();
            movementDataHolder.ValueRW.movingBattalions.Clear();
            movementDataHolder.ValueRW.plannedMovementDirections.Clear();
            movementDataHolder.ValueRW.blockers.Clear();
            movementDataHolder.ValueRW.battalionFollowers.Clear();
            movementDataHolder.ValueRW.waitingForSoldiersBattalions.Clear();
            movementDataHolder.ValueRW.battalionExactDistance.Clear();

            backupPlanDataHolder.ValueRW.battleChunksPerRowTeam.Clear();
            backupPlanDataHolder.ValueRW.emptyChunks.Clear();
            backupPlanDataHolder.ValueRW.moveLeft.Clear();
            backupPlanDataHolder.ValueRW.moveRight.Clear();
            backupPlanDataHolder.ValueRW.moveToDifferentChunk.Clear();
            backupPlanDataHolder.ValueRW.chunkLinks.Clear();
            backupPlanDataHolder.ValueRW.allChunks.Clear();
            backupPlanDataHolder.ValueRW.chunksNeedingReinforcements.Clear();
            backupPlanDataHolder.ValueRW.chunkReinforcementPaths.Clear();
            backupPlanDataHolder.ValueRW.battalionIdToChunk.Clear();
            backupPlanDataHolder.ValueRW.lastChunkId = 0;
        }
    }
}