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

namespace system.battle.battalion.analysis.horizontal_split
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    [UpdateAfter(typeof(FindFightingPairsSystem))]
    public partial struct HS1_FindHorizontalSplitBlockers : ISystem
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
            var blockedHorizontalSplits = DataHolder.blockedHorizontalSplits;

            foreach (var rowId in allRows)
            {
                (long, float3, Team, float, BattleUnitTypeEnum)? leftUnitOptional = null;

                foreach (var me in positions.GetValuesForKey(rowId))
                {
                    //unit is the most left, there is noone to compare with
                    if (!leftUnitOptional.HasValue)
                    {
                        leftUnitOptional = me;
                        continue;
                    }

                    var leftUnit = leftUnitOptional.Value;
                    leftUnitOptional = me;

                    //keep in mind that both battalions can have different size, so check has to be done for each battalion separatelly
                    var canISplitLeft = BattleTransformUtils.isTooFarForSplit(me.Item2, leftUnit.Item2, me.Item4, leftUnit.Item4);
                    if (!canISplitLeft)
                    {
                        blockedHorizontalSplits.Add(me.Item1, Direction.LEFT);
                    }

                    var canLeftUnitSplitRight = BattleTransformUtils.isTooFarForSplit(leftUnit.Item2, me.Item2, leftUnit.Item4, me.Item4);
                    if (!canLeftUnitSplitRight)
                    {
                        blockedHorizontalSplits.Add(leftUnit.Item1, Direction.RIGHT);
                    }
                }
            }
        }
    }
}