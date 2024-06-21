using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
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
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();

            var positions = dataHolder.ValueRO.positions;
            var blockers = movementDataHolder.ValueRO.blockers;
            var allRows = dataHolder.ValueRO.allRowIds;

            foreach (var rowId in allRows)
            {
                (long, float3, Team, float, BattleUnitTypeEnum)? leftUnitOptional = null;

                foreach (var me in positions.GetValuesForKey(rowId))
                {
                    if (rowId != 0)
                    {
                        findDiagonalBlockers(me, rowId, blockers, dataHolder.ValueRO);
                    }

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
                }
            }

            createFollowers(movementDataHolder);
        }

        private void findDiagonalBlockers(
            (long, float3, Team, float, BattleUnitTypeEnum) me,
            int rowId,
            NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers,
            DataHolder dataHolder)
        {
            var positions = dataHolder.positions;
            foreach (var upper in positions.GetValuesForKey(rowId - 1))
            {
                var isTooFarDiagonal = BattleTransformUtils.isTooFar(me.Item2, upper.Item2, me.Item4, upper.Item4);

                if (!isTooFarDiagonal)
                {
                    addBlockerVertical(me, upper, blockers);
                }
            }
        }

        private void addBlocker((long, float3, Team, float, BattleUnitTypeEnum) right, (long, float3, Team, float, BattleUnitTypeEnum) left,
            NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers)
        {
            //left to right
            blockers.Add(left.Item1, (right.Item1, right.Item5, Direction.RIGHT, right.Item3));
            //right to left
            blockers.Add(right.Item1, (left.Item1, left.Item5, Direction.LEFT, left.Item3));
        }

        private void addBlockerVertical((long, float3, Team, float, BattleUnitTypeEnum) bottom, (long, float3, Team, float, BattleUnitTypeEnum) upper,
            NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers)
        {
            //upper to down
            blockers.Add(upper.Item1, (bottom.Item1, bottom.Item5, Direction.DOWN, bottom.Item3));
            //down to top
            blockers.Add(bottom.Item1, (upper.Item1, upper.Item5, Direction.UP, upper.Item3));
        }


        private void createFollowers(RefRW<MovementDataHolder> movementDataHolder)
        {
            var blockers = movementDataHolder.ValueRO.blockers;
            var battalionFollowers = movementDataHolder.ValueRW.battalionFollowers;

            foreach (var blocked in blockers)
            {
                battalionFollowers.Add(blocked.Value.Item1, (blocked.Key, blocked.Value.Item3));
            }
        }
    }
}