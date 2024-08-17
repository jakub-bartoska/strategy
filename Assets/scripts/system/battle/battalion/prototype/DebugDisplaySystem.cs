using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.cleanup
{
    public partial struct DebugDisplaySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DisplayError
                {
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct DisplayError : IJobEntity
    {
        private void Execute(BattalionMarker arrowMarkerDebug)
        {
        }
    }
}