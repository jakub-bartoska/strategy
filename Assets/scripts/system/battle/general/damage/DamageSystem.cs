using component;
using component._common.system_switchers;
using component.helpers;
using component.soldier;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace system.general
{
    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Damage>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var damageBuffer = SystemAPI.GetSingletonBuffer<Damage>();

            new DamageJob
                {
                    damageBuffer = damageBuffer,
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            damageBuffer.Clear();
        }
    }

    [BurstCompile]
    public partial struct DamageJob : IJobEntity
    {
        [ReadOnly] public DynamicBuffer<Damage> damageBuffer;
        [NativeDisableUnsafePtrRestriction] public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute(ref SoldierHp soldierHp, Entity entity, SoldierStatus soldierStatus)
        {
            var dmgReceived = 0;
            foreach (var damage in damageBuffer)
            {
                if (damage.dmgReceiverId == soldierStatus.index)
                {
                    dmgReceived += damage.dmgAmount;
                }
            }

            if (dmgReceived == 0) return;

            soldierHp.hp -= dmgReceived;
            if (soldierHp.hp <= 0)
            {
                ecb.DestroyEntity(soldierStatus.index, entity);
            }
        }
    }
}