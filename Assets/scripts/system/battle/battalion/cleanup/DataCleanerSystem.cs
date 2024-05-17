using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
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
            var battleDataHolder = DataHolder.positions;
            battleDataHolder.Clear();

            var blockers = DataHolder.blockers;
            blockers.Clear();

            var battalionDefaultMovementDirection = DataHolder.battalionDefaultMovementDirection;
            battalionDefaultMovementDirection.Clear();

            var battalionFollowers = DataHolder.battalionFollowers;
            battalionFollowers.Clear();

            var fightingPairs = DataHolder.fightingPairs;
            fightingPairs.Clear();

            var notMovingBattalions = DataHolder.notMovingBattalions;
            notMovingBattalions.Clear();

            var needReinforcements = DataHolder.needReinforcements;
            needReinforcements.Clear();

            var allBattalionIds = DataHolder.allBattalionIds;
            allBattalionIds.Clear();

            var reinforcements = DataHolder.reinforcements;
            reinforcements.Clear();

            var flankPositions = DataHolder.flankPositions;
            flankPositions.Clear();

            var flankingBattalions = DataHolder.flankingBattalions;
            flankingBattalions.Clear();

            var allRowIds = DataHolder.allRowIds;

            if (allRowIds.IsEmpty)
            {
                for (int i = 0; i < 10; i++)
                {
                    allRowIds.Add(i);
                }
            }
        }
    }
}