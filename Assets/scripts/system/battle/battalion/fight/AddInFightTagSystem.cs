using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.fight
{
    [UpdateAfter(typeof(AnalyseBattlefieldSystem))]
    public partial struct AddInFightTagSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var fightPairs = SystemAPI.GetSingletonBuffer<FightPair>();
            new AddInBattleTagJob
                {
                    fightPairs = fightPairs
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct AddInBattleTagJob : IJobEntity
        {
            [ReadOnly] public DynamicBuffer<FightPair> fightPairs;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionFightBuffer> battalionFight)
            {
                foreach (var fightPair in fightPairs)
                {
                    if (battalionMarker.id != fightPair.battalionId1 && battalionMarker.id != fightPair.battalionId2) continue;

                    var enemyId = battalionMarker.id == fightPair.battalionId1 ? fightPair.battalionId2 : fightPair.battalionId1;
                    var exists = false;
                    foreach (var fight in battalionFight)
                    {
                        if (fight.enemyBattalionId == enemyId)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        battalionFight.Add(new BattalionFightBuffer
                        {
                            time = 1f,
                            enemyBattalionId = enemyId,
                            type = fightPair.fightType
                        });
                    }
                }
            }
        }
    }
}