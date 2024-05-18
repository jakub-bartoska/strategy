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
            DataHolder.positions.Clear();
            DataHolder.blockers.Clear();
            DataHolder.battalionDefaultMovementDirection.Clear();
            DataHolder.battalionFollowers.Clear();
            DataHolder.fightingPairs.Clear();
            DataHolder.battalionsPerformingAction.Clear();
            DataHolder.needReinforcements.Clear();
            DataHolder.allBattalionIds.Clear();
            DataHolder.reinforcements.Clear();
            DataHolder.flankPositions.Clear();
            DataHolder.flankingBattalions.Clear();
            DataHolder.rowChanges.Clear();
            DataHolder.battalionSwitchRowDirections.Clear();

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