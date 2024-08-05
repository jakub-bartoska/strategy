using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_0_AddChunkLinks))]
    public partial struct CHM3_1_FindNeedReinforcementChunks : ISystem
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
            var allChunks = backupPlanDataHolder.ValueRO.allChunks;
            var chunksNeedingReinforcements = backupPlanDataHolder.ValueRW.chunksNeedingReinforcements;

            foreach (var idChunk in allChunks)
            {
                var chunk = idChunk.Value;
                var neededBattalions = 0;
                if (chunk.leftFighting)
                {
                    neededBattalions++;
                }

                if (chunk.rightFighting)
                {
                    neededBattalions++;
                }

                if (chunk.battalions.Length < neededBattalions)
                {
                    chunksNeedingReinforcements.Add(chunk.chunkId);
                }
            }
        }
    }
}