using component._common.system_switchers;
using component.battle.battalion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(BattalionMovementSystem))]
    public partial struct BattalionFightSystem : ISystem
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
            var deltaTime = SystemAPI.Time.DeltaTime;

            var damageDealt = new NativeParallelHashMap<long, int>(1000, Allocator.TempJob);

            new PerformBattalionFightJob
                {
                    deltaTime = deltaTime,
                    damageDealt = damageDealt.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new ReceiveDamageJob
                {
                    damageDealt = damageDealt,
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct PerformBattalionFightJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            public NativeParallelHashMap<long, int>.ParallelWriter damageDealt;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionFightBuffer> battalionFight, DynamicBuffer<BattalionSoldiers> soldiers)
            {
                if (battalionFight.Length == 0) return;

                for (var i = 0; i < battalionFight.Length; i++)
                {
                    var time = battalionFight[i].time - deltaTime;
                    if (time <= 0)
                    {
                        time += 1;
                        damageDealt.TryAdd(battalionFight[i].enemyBattalionId, soldiers.Length);
                    }

                    var newFight = new BattalionFightBuffer
                    {
                        time = time,
                        enemyBattalionId = battalionFight[i].enemyBattalionId
                    };
                    battalionFight[i] = newFight;
                }
            }
        }

        [BurstCompile]
        public partial struct ReceiveDamageJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<long, int> damageDealt;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(BattalionMarker battalionMarker, ref BattalionHealth health, ref DynamicBuffer<BattalionSoldiers> soldiers, Entity entity)
            {
                if (damageDealt.TryGetValue(battalionMarker.id, out var damage))
                {
                    health.value -= damage;
                    if (health.value <= 0)
                    {
                        Debug.Log("suicide");
                        foreach (var soldier in soldiers)
                        {
                            ecb.DestroyEntity(1, soldier.entity);
                        }

                        ecb.DestroyEntity(2, entity);
                        return;
                    }

                    Debug.Log("health left: " + health.value);
                    var soldiersToKill = soldiers.Length - 1 - (health.value / 10);
                    var originalLength = soldiers.Length;
                    for (var i = 0; i < soldiersToKill; i++)
                    {
                        var index = originalLength - 1 - i;
                        ecb.DestroyEntity(1, soldiers[index].entity);
                        soldiers.RemoveAt(index);
                    }
                }
            }
        }
    }
}