using component._common.system_switchers;
using component.battle.battalion.data_holders;
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
            state.RequireForUpdate<MovementDataHolder>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingleton<MovementDataHolder>();
            var needReinforcements = dataHolder.ValueRW.needReinforcements;
            foreach (var movingBattalion in movementDataHolder.movingBattalions)
            {
                needReinforcements.Remove(movingBattalion.Key);
            }
        }
    }
}