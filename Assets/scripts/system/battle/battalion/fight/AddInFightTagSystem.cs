using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.enums;
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

            var splitCandidateList = new NativeList<SplitCandidate>(1000, Allocator.TempJob);

            new AddInBattleTagJob
                {
                    fightPairs = fightPairs,
                    splitCandidateList = splitCandidateList.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            var splitCandidates = SystemAPI.GetSingletonBuffer<SplitCandidate>();
            splitCandidates.Clear();
            splitCandidates.AddRange(splitCandidateList);
        }

        [BurstCompile]
        public partial struct AddInBattleTagJob : IJobEntity
        {
            [ReadOnly] public DynamicBuffer<FightPair> fightPairs;
            public NativeList<SplitCandidate>.ParallelWriter splitCandidateList;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionFightBuffer> battalionFight, BattalionTeam team)
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

                    if (fightPair.fightType != BattalionFightType.VERTICAL) continue;

                    var direction = team.value switch
                    {
                        Team.TEAM1 => Direction.LEFT,
                        Team.TEAM2 => Direction.RIGHT,
                        _ => throw new System.NotImplementedException()
                    };
                    splitCandidateList.AddNoResize(new SplitCandidate
                    {
                        battalionId = battalionMarker.id,
                        direction = direction,
                        type = SplitType.MINUS_TWO
                    });
                }
            }
        }
    }
}