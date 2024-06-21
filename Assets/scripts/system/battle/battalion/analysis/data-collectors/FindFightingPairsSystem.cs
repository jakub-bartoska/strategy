using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
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
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var positions = dataHolder.ValueRO.positions;
            var allRows = dataHolder.ValueRO.allRowIds;

            foreach (var rowId in allRows)
            {
                (long, float3, Team, float, BattleUnitTypeEnum)? leftUnitOptional = null;
                foreach (var me in positions.GetValuesForKey(rowId))
                {
                    if (rowId != 0)
                    {
                        findDiagonalFightingPairs(me, rowId, dataHolder);
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
                        addFightingPair(me.Item1, leftUnit.Item1, BattalionFightType.NORMAL, dataHolder);
                    }
                }
            }
        }

        private void findDiagonalFightingPairs((long, float3, Team, float, BattleUnitTypeEnum) me, int rowId, RefRW<DataHolder> dataHolder)
        {
            var positions = dataHolder.ValueRO.positions;
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
                    addFightingPair(me.Item1, bellow.Item1, BattalionFightType.VERTICAL, dataHolder);
                }
            }
        }

        private void addFightingPair(long battalionId1, long battalionId2, BattalionFightType fightType, RefRW<DataHolder> dataHolder)
        {
            dataHolder.ValueRW.fightingPairs.Add((battalionId1, battalionId2, fightType));
            dataHolder.ValueRW.battalionsPerformingAction.Add(battalionId1);
            dataHolder.ValueRW.battalionsPerformingAction.Add(battalionId2);
            dataHolder.ValueRW.fightingBattalions.Add(battalionId1);
            dataHolder.ValueRW.fightingBattalions.Add(battalionId2);
        }
    }
}