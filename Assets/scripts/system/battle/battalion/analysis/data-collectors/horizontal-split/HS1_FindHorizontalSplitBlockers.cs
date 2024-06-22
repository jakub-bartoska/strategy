using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.battalion.execution.movement;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.battle.battalion.analysis.horizontal_split
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(PositionParserSystem))]
    [UpdateAfter(typeof(FindFightingPairsSystem))]
    [UpdateAfter(typeof(MD3_OverrideByFlanks))]
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
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var positions = dataHolder.ValueRO.positions;
            var allRows = dataHolder.ValueRO.allRowIds;
            var blockedHorizontalSplits = dataHolder.ValueRW.blockedHorizontalSplits;

            foreach (var rowId in allRows)
            {
                BattalionInfo? leftUnitOptional = null;

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
                    var canISplitLeft = BattleTransformUtils.isTooFarForSplit(me.position, leftUnit.position, me.width, leftUnit.width);
                    if (!canISplitLeft)
                    {
                        blockedHorizontalSplits.Add(me.battalionId, Direction.LEFT);
                    }

                    var canLeftUnitSplitRight = BattleTransformUtils.isTooFarForSplit(leftUnit.position, me.position, leftUnit.width, me.width);
                    if (!canLeftUnitSplitRight)
                    {
                        blockedHorizontalSplits.Add(leftUnit.battalionId, Direction.RIGHT);
                    }
                }
            }
        }
    }
}