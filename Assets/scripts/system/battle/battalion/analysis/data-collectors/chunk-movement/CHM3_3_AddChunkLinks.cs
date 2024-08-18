using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_2_ChangeChunkSize))]
    public partial struct CHM3_3_AddChunkLinks : ISystem
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
            var chunkLinks = backupPlanDataHolder.ValueRW.chunkLinks;

            foreach (var idChunk in allChunks)
            {
                var chunk = idChunk.Value;
                AddChunkLinks(chunk, backupPlanDataHolder.ValueRO.battleChunksPerRowTeam, backupPlanDataHolder.ValueRO.emptyChunks, allChunks, chunkLinks);
            }
        }

        private void AddChunkLinks(
            BattleChunk myChunk,
            NativeParallelMultiHashMap<TeamRow, long> filledChunks,
            NativeParallelMultiHashMap<TeamRow, long> emptyChunks,
            NativeHashMap<long, BattleChunk> allChunks,
            NativeParallelMultiHashMap<long, long> chunkLinks)
        {
            findNeighboursForRow(myChunk, filledChunks, emptyChunks, allChunks, chunkLinks, -1);
            findNeighboursForRow(myChunk, filledChunks, emptyChunks, allChunks, chunkLinks, 1);
        }

        private void findNeighboursForRow(BattleChunk myChunk,
            NativeParallelMultiHashMap<TeamRow, long> filledChunks,
            NativeParallelMultiHashMap<TeamRow, long> emptyChunks,
            NativeHashMap<long, BattleChunk> allChunks,
            NativeParallelMultiHashMap<long, long> chunkLinks,
            int rowDelta)
        {
            var neighbourRowKey = new TeamRow
            {
                rowId = myChunk.rowId + rowDelta,
                team = myChunk.team
            };
            foreach (var chunkId in filledChunks.GetValuesForKey(neighbourRowKey))
            {
                var neighbourChunk = allChunks[chunkId];
                var neighbouring = areChunksNeigbouring(myChunk, neighbourChunk);
                if (!neighbouring) continue;

                chunkLinks.Add(myChunk.chunkId, neighbourChunk.chunkId);
            }

            foreach (var chunkId in emptyChunks.GetValuesForKey(neighbourRowKey))
            {
                var neighbourChunk = allChunks[chunkId];
                var neighbouring = areChunksNeigbouring(myChunk, neighbourChunk);
                if (!neighbouring) continue;

                chunkLinks.Add(myChunk.chunkId, neighbourChunk.chunkId);
            }
        }

        private bool areChunksNeigbouring(BattleChunk chunk1, BattleChunk chunk2)
        {
            if (chunk1.startX < chunk2.startX)
            {
                // A--A
                //       B--B
                if (chunk1.endX <= chunk2.startX)
                {
                    return false;
                }

                // A----A
                //    B--B

                // A---------A
                //    B--B
                return true;
            }

            //    A----A
            // B----B

            //    A-A
            // B------B
            if (chunk1.startX < chunk2.endX)
            {
                return true;
            }

            //      A-A
            // B-B
            return false;
        }
    }
}