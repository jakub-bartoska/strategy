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
            var battleDataHolder = BattleUnitDataHolder.positions;
            battleDataHolder.Clear();

            var blockers = BattleUnitDataHolder.blockers;
            blockers.Clear();

            var battalionDefaultMovementDirection = BattleUnitDataHolder.battalionDefaultMovementDirection;
            battalionDefaultMovementDirection.Clear();

            var battalionFollowers = BattleUnitDataHolder.battalionFollowers;
            battalionFollowers.Clear();

            var fightingPairs = BattleUnitDataHolder.fightingPairs;
            fightingPairs.Clear();

            var notMovingBattalions = BattleUnitDataHolder.notMovingBattalions;
            notMovingBattalions.Clear();

            var allRowIds = BattleUnitDataHolder.allRowIds;

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