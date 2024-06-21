using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.battle.config;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    public partial struct FightSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var debugConfig = SystemAPI.GetSingleton<DebugConfig>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            //dmg delaed by 1 soldier per second (keep in mind that battalion can have 10 soldiers)
            var dmgPerSoldier = debugConfig.dmgPerSecond;
            var dmgPerPerSoldierPerDeltaTime = dmgPerSoldier * deltaTime;

            if (!debugConfig.doDamage)
            {
                return;
            }

            //battalion id - soldier count per battalion
            var soldierCountsPerBattalion = new NativeHashMap<long, int>(1000, Allocator.TempJob); //ok
            new CollectSoldierCountPerBattalionJob
                {
                    soldierCountPerBattalion = soldierCountsPerBattalion
                }.Schedule(state.Dependency)
                .Complete();

            //received damage - battalionId, dmg
            //can contain multiple dmg since 1 battalion could be attacked by multiple enemies
            var dmgReceived = new NativeParallelMultiHashMap<long, float>(1000, Allocator.TempJob); //ok

            foreach (var fightingPair in dataHolder.ValueRO.fightingPairs)
            {
                // battalion1 damages battalion2
                var dmgToReceive = fightingPair.Item3 switch
                {
                    //todo add soldier count
                    BattalionFightType.NORMAL => dmgPerPerSoldierPerDeltaTime * soldierCountsPerBattalion[fightingPair.Item1],
                    BattalionFightType.VERTICAL => dmgPerPerSoldierPerDeltaTime,
                    _ => throw new Exception("unknown fight type"),
                };
                dmgReceived.Add(fightingPair.Item2, dmgToReceive);

                // battalion2 damages battalion1
                dmgToReceive = fightingPair.Item3 switch
                {
                    //todo add soldier count
                    BattalionFightType.NORMAL => dmgPerPerSoldierPerDeltaTime * soldierCountsPerBattalion[fightingPair.Item2],
                    BattalionFightType.VERTICAL => dmgPerPerSoldierPerDeltaTime,
                    _ => throw new Exception("unknown fight type"),
                };
                dmgReceived.Add(fightingPair.Item1, dmgToReceive);
            }

            new PerformBattalionFightJob
                {
                    dmgReceived = dmgReceived
                }.Schedule(state.Dependency)
                .Complete();

            soldierCountsPerBattalion.Dispose();
            dmgReceived.Dispose();
        }

        [BurstCompile]
        public partial struct CollectSoldierCountPerBattalionJob : IJobEntity
        {
            public NativeHashMap<long, int> soldierCountPerBattalion;

            private void Execute(BattalionMarker battalionMarker, DynamicBuffer<BattalionSoldiers> soldiers)
            {
                soldierCountPerBattalion.Add(battalionMarker.id, soldiers.Length);
            }
        }

        [BurstCompile]
        public partial struct PerformBattalionFightJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, float> dmgReceived;

            private void Execute(BattalionMarker battalionMarker, ref BattalionHealth health)
            {
                foreach (var dmgAmount in dmgReceived.GetValuesForKey(battalionMarker.id))
                {
                    health.value -= dmgAmount;
                }
            }
        }
    }
}