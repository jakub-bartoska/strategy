using component._common.system_switchers;
using component.battle.battalion;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace system.battle.battalion.cleanup
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    public partial struct DebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DebugJob()
                {
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct DebugJob : IJobEntity
    {
        private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers)
        {
            if (soldiers.Length > 10)
            {
                Debug.Log("Je nas moc: " + soldiers.Length);
            }
        }
    }
}