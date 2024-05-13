using component._common.system_switchers;
using component.battle.battalion;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateInGroup(typeof(BattleSetResultsSystemGroup))]
    public partial struct DestroyKilledSoldiersSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new DestroyKilledSoldiersJob
                {
                    ecb = ecb
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(BattalionMarker))]
        public partial struct DestroyKilledSoldiersJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            private void Execute(ref DynamicBuffer<BattalionSoldiers> soldiers, BattalionHealth health)
            {
                var soldiersToDestroy = soldiers.Length - (int) (health.value / 10) - 1;
                if (soldiersToDestroy == 0)
                {
                    return;
                }
                //some soldiers needs to be killed

                for (var i = 0; i < soldiersToDestroy; i++)
                {
                    ecb.DestroyEntity(soldiers[0].entity);
                    soldiers.RemoveAt(0);
                }
            }
        }
    }
}