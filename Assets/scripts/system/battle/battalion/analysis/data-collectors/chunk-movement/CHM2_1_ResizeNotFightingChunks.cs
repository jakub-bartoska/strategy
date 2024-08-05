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
                var leftX = chunk.leftFighting ? chunk.startX : CustomTransformUtils.defaulBattleMapOffset.x - CustomTransformUtils.battleXSize;
                var rightX = chunk.rightFighting ? chunk.endX : CustomTransformUtils.defaulBattleMapOffset.x + CustomTransformUtils.battleXSize;
                if (!chunk.leftFighting || !chunk.rightFighting)
                {
                    var newChunk = new BattleChunk
                    {
                        team = chunk.team,
                        battalions = chunk.battalions,
                        startX = leftX,
                        endX = rightX,
                        leftFighting = chunk.leftFighting,
                        rightFighting = chunk.rightFighting,
                        rowId = chunk.rowId,
                        chunkId = chunk.chunkId
                    };
                    allChunks[chunkKey] = newChunk;
                }
            }
        }
    }
}