using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.backup_plans
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(CHM3_0_FindNeedReinforcementChunks))]
    public partial struct CHM3_2_ChangeChunkSize : ISystem
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
            var battleChunksPerRowTeam = backupPlanDataHolder.ValueRO.battleChunksPerRowTeam;
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var fightingBattalions = dataHolder.ValueRW.fightingBattalions;
            var allBattalions = dataHolder.ValueRO.battalionInfo;

            foreach (var chunkId in battleChunksPerRowTeam.GetValueArray(Allocator.Temp))
            {
                var chunk = allChunks[chunkId];
                var newChunkValue = adjustChunkByRightSide(chunk, fightingBattalions, allBattalions);

                allChunks[chunkId] = newChunkValue;
            }

            //proiterovat chunky co fightujou na jedny i druhy strane
            //pokud jsou zakonceny fightujicim battalionem, tak zmensit
            //pokud je navic za fightujicim battalionem posila blockla v smeru, tak tu taky odstranit
        }

        private BattleChunk adjustChunkByRightSide(BattleChunk originalChunk, NativeHashSet<long> fightingBattalions, NativeHashMap<long, BattalionInfo> allBattalions)
        {
            var chunkBattalions = originalChunk.battalions;
            if (chunkBattalions.Length == 0)
            {
                return originalChunk;
            }

            var sortedBattalions = new NativeList<BattalionInfo>(chunkBattalions.Length, Allocator.Temp);
            foreach (var id in chunkBattalions)
            {
                sortedBattalions.Add(allBattalions[id]);
            }

            var ascSorter = new SortByPosition();
            sortedBattalions.Sort(ascSorter);

            var isFightingRight = fightingBattalions.Contains(sortedBattalions[sortedBattalions.Length - 1].battalionId);
            var isFightingLeft = fightingBattalions.Contains(sortedBattalions[0].battalionId);

            var newRightXPosition = originalChunk.endX;
            var newLeftXPosition = originalChunk.startX;

            if (isFightingRight && originalChunk.rightFighting)
            {
                var mostRightBattalion = sortedBattalions[sortedBattalions.Length - 1];
                newRightXPosition = mostRightBattalion.position.x - mostRightBattalion.width / 2;
            }

            if (isFightingLeft && originalChunk.leftFighting)
            {
                var mostLeftPosition = sortedBattalions[0];
                newLeftXPosition = mostLeftPosition.position.x + mostLeftPosition.width / 2;
            }

            return new BattleChunk
            {
                chunkId = originalChunk.chunkId,
                rowId = originalChunk.rowId,
                team = originalChunk.team,
                leftFighting = originalChunk.leftFighting,
                rightFighting = originalChunk.rightFighting,
                battalions = originalChunk.battalions,
                startX = newLeftXPosition,
                endX = newRightXPosition
            };
        }
    }
}