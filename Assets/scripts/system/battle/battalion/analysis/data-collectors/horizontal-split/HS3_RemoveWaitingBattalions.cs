using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.horizontal_split
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(HS2_FindSplitCandidates))]
    public partial struct HS3_RemoveWaitingBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var waitingForSoldiersBattalions = MovementDataHolder.waitingForSoldiersBattalions;
            var splitBattalions = DataHolder.splitBattalions;

            foreach (var waitingForSoldiersBattalion in waitingForSoldiersBattalions)
            {
                splitBattalions.Remove(waitingForSoldiersBattalion);
            }
        }
    }
}