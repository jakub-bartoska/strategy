using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM2_0_AddEmptyChunks))]
    public partial struct CHM2_1_ResizeNotFightingChunks : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var backupPlanDataHolder = SystemAPI.GetSingletonRW<BackupPlanDataHolder>();
            var allChunks = backupPlanDataHolder.ValueRW.allChunks;
            foreach (var chunkKey in allChunks.GetKeyArray(Allocator.Temp))
            {
                var chunk = allChunks[chunkKey];
                var leftX = chunk.leftEnemy.HasValue ? chunk.startX : CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize;
                var rightX = chunk.rightEnemy.HasValue ? chunk.endX : CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize;
                if (!chunk.leftEnemy.HasValue || !chunk.rightEnemy.HasValue)
                {
                    var newChunk = new BattleChunk
                    {
                        team = chunk.team,
                        battalions = chunk.battalions,
                        startX = leftX,
                        endX = rightX,
                        leftEnemy = chunk.leftEnemy,
                        rightEnemy = chunk.rightEnemy,
                        rowId = chunk.rowId,
                        chunkId = chunk.chunkId
                    };
                    allChunks[chunkKey] = newChunk;
                }
            }
        }
    }
}