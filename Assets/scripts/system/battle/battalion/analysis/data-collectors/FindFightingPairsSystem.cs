using component;
using component._common.system_switchers;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.utils;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    public partial struct FindFightingPairsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var positions = DataHolder.positions;
            var allRows = DataHolder.allRowIds;

            foreach (var rowId in allRows)
            {
                (long, float3, Team, float, BattleUnitTypeEnum)? leftUnitOptional = null;
                foreach (var me in positions.GetValuesForKey(rowId))
                {
                    if (rowId != 0)
                    {
                        findDiagonalFightingPairs(me, rowId);
                    }

                    //unit is the most left, there is noone to compare with
                    if (!leftUnitOptional.HasValue)
                    {
                        leftUnitOptional = me;
                        continue;
                    }

                    var leftUnit = leftUnitOptional.Value;
                    leftUnitOptional = me;

                    //ignore same team
                    if (me.Item3 == leftUnit.Item3)
                    {
                        continue;
                    }

                    var isTooFar = BattleTransformUtils.isTooFar(me.Item2, leftUnit.Item2, me.Item4, leftUnit.Item4);
                    if (!isTooFar)
                    {
                        addFightingPair(me.Item1, leftUnit.Item1, BattalionFightType.NORMAL);
                    }
                }
            }
        }

        private void findDiagonalFightingPairs((long, float3, Team, float, BattleUnitTypeEnum) me, int rowId)
        {
            var positions = DataHolder.positions;
            foreach (var bellow in positions.GetValuesForKey(rowId - 1))
            {
                var isTooFarDiagonal = BattleTransformUtils.isTooFar(me.Item2, bellow.Item2, me.Item4, bellow.Item4, 0.5f);
                //skip same team
                if (me.Item3 == bellow.Item3)
                {
                    continue;
                }

                if (!isTooFarDiagonal)
                {
                    addFightingPair(me.Item1, bellow.Item1, BattalionFightType.VERTICAL);
                }
            }
        }

        private void addFightingPair(long battalionId1, long battalionId2, BattalionFightType fightType)
        {
            DataHolder.fightingPairs.Add((battalionId1, battalionId2, fightType));
            DataHolder.battalionsPerformingAction.Add(battalionId1);
            DataHolder.battalionsPerformingAction.Add(battalionId2);
            DataHolder.fightingBattalions.Add(battalionId1);
            DataHolder.fightingBattalions.Add(battalionId2);
        }
    }
}