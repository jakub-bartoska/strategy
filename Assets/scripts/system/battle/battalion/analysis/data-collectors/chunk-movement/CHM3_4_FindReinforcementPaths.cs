using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_3_AddChunkLinks))]
    public partial struct CHM3_4_FindReinforcementPaths : ISystem
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
            var needReinforcements = backupPlanDataHolder.ValueRO.chunksNeedingReinforcements;
            var result = backupPlanDataHolder.ValueRW.chunkReinforcementPaths;
            var chunkLinks = backupPlanDataHolder.ValueRO.chunkLinks;

            foreach (var needReinforcement in needReinforcements)
            {
                result.Add(needReinforcement.Key, new ChunkPath
                {
                    pathType = PathType.TARGET,
                    targetChunkBattalionCount = needReinforcement.Value,
                    pathLength = 0,
                });
            }

            foreach (var needReinforcement in needReinforcements)
            {
                findPaths(needReinforcement.Key, result, 1, needReinforcement.Value, chunkLinks);
            }

            var allChunks = backupPlanDataHolder.ValueRO.allChunks;
            foreach (var chunk in allChunks)
            {
                if (!result.ContainsKey(chunk.Key))
                {
                    result.Add(chunk.Key, new ChunkPath
                    {
                        pathType = PathType.NO_VALID_PATH,
                    });
                }
            }
        }

        private void findPaths(long currentChunk, NativeHashMap<long, ChunkPath> result, int pathDepth, int targetBattalionCount, NativeParallelMultiHashMap<long, long> chunkLinks)
        {
            foreach (var neighbour in chunkLinks.GetValuesForKey(currentChunk))
            {
                if (result.TryGetValue(neighbour, out var oldPath))
                {
                    if (pathToCoeficient(oldPath.pathLength, oldPath.targetChunkBattalionCount) >= pathToCoeficient(pathDepth, targetBattalionCount))
                    {
                        continue;
                    }
                }

                result[neighbour] = new ChunkPath
                {
                    pathType = PathType.PATH,
                    targetChunkId = currentChunk,
                    targetChunkBattalionCount = targetBattalionCount,
                    pathLength = pathDepth,
                };

                findPaths(neighbour, result, pathDepth + 1, targetBattalionCount, chunkLinks);
            }
        }

        private float pathToCoeficient(int pathLength, int targetBattalionCount)
        {
            return 1f / (1f + pathLength) / (1f + targetBattalionCount * 1.5f);
        }
    }
}