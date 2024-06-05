using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.soldiers;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.execution.reinforcement
{
    /**
     * Moving battalions are not able to receive reinforcements
     */
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(M4_SoldiersFollowBattalionSystem))]
    public partial struct R1_RemoveMovingBattalionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var needReinforcements = DataHolder.needReinforcements;
            foreach (var movingBattalion in MovementDataHolder.movingBattalions)
            {
                needReinforcements.Remove(movingBattalion.Key);
            }
        }
    }
}