using component;
using component._common.system_switchers;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.utils;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace system.battle.battalion.analysis
{
    [UpdateAfter(typeof(PositionParserSystem))]
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    public partial struct FindBlockerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var positions = BattleUnitDataHolder.positions;
            var blockers = BattleUnitDataHolder.blockers;
            var allRows = BattleUnitDataHolder.allRowIds;

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

                    var isTooFar = BattleTransformUtils.isTooFar(me.Item2, leftUnit.Item2, me.Item4, leftUnit.Item4);
                    if (!isTooFar)
                    {
                        addBlocker(me, leftUnit, blockers);
                    }

                    //compare to units in row above
                }
            }

            createFollowers();
        }

        private void addBlocker((long, float3, Team, float, BattleUnitTypeEnum) right, (long, float3, Team, float, BattleUnitTypeEnum) left,
            NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction)> blockers)
        {
            //left to right
            blockers.Add(left.Item1, (right.Item1, right.Item5, Direction.RIGHT));
            //right to left
            blockers.Add(right.Item1, (left.Item1, left.Item5, Direction.LEFT));
        }

        private void createFollowers()
        {
            var blockers = BattleUnitDataHolder.blockers;
            var battalionFollowers = BattleUnitDataHolder.battalionFollowers;

            foreach (var blocked in blockers)
            {
                battalionFollowers.Add(blocked.Value.Item1, (blocked.Key, blocked.Value.Item3));
            }
        }
    }
}