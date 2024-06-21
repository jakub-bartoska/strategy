using component._common.system_switchers;
using component.battle.battalion.data_holders;
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
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var waitingForSoldiersBattalions = movementDataHolder.ValueRO.waitingForSoldiersBattalions;
            var splitBattalions = dataHolder.ValueRW.splitBattalions;

            foreach (var waitingForSoldiersBattalion in waitingForSoldiersBattalions)
            {
                splitBattalions.Remove(waitingForSoldiersBattalion);
            }
        }
    }
}