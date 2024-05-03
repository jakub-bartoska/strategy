using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.config;
using system.battle.battalion.fight;
using system.battle.enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(AddInFightTagSystem))]
    public partial struct FightSystem : ISystem
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

            var doDamage = SystemAPI.GetSingleton<DebugConfig>().doDamage;
            if (!doDamage)
            {
                return;
            }

            new ReceiveDamageJob
                {
                    damageDealt = damageDealt,
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        [WithAll(typeof(BattalionMarker))]
        public partial struct PerformBattalionFightJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            public NativeParallelHashMap<long, int>.ParallelWriter damageDealt;

            private void Execute(ref DynamicBuffer<BattalionFightBuffer> battalionFight, DynamicBuffer<BattalionSoldiers> soldiers)
            {
                if (battalionFight.Length == 0) return;

                for (var i = 0; i < battalionFight.Length; i++)
                {
                    var time = battalionFight[i].time - deltaTime;
                    if (time <= 0)
                    {
                        time += 1;
                        switch (battalionFight[i].type)
                        {
                            case BattalionFightType.NORMAL:
                                damageDealt.TryAdd(battalionFight[i].enemyBattalionId, soldiers.Length);
                                break;
                            case BattalionFightType.VERTICAL:
                                //spocitat ci bocni jednotky
                                damageDealt.TryAdd(battalionFight[i].enemyBattalionId, 1);
                                break;
                            default:
                                throw new Exception("Unknown battalion fight type");
                        }
                    }

                    var newFight = new BattalionFightBuffer
                    {
                        time = time,
                        enemyBattalionId = battalionFight[i].enemyBattalionId,
                        type = battalionFight[i].type
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
                        foreach (var soldier in soldiers)
                        {
                            ecb.DestroyEntity(1, soldier.entity);
                        }

                        ecb.DestroyEntity(2, entity);
                        return;
                    }

                    var soldiersToKill = soldiers.Length - 1 - (health.value / 10);
                    for (var i = 0; i < soldiersToKill; i++)
                    {
                        ecb.DestroyEntity(1, soldiers[0].entity);
                        soldiers.RemoveAt(0);
                    }
                }
            }
        }
    }
}