using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion
{
    public partial struct FlankSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}