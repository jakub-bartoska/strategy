using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;

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
                BattalionInfo? leftUnitOptional = null;
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
                    if (me.team == leftUnit.team)
                    {
                        continue;
                    }

                    var isTooFar = BattleTransformUtils.isTooFar(me.position, leftUnit.position, me.width, leftUnit.width);
                    if (!isTooFar)
                    {
                        addFightingPair(me.battalionId, leftUnit.battalionId, BattalionFightType.NORMAL, dataHolder);
                    }
                }
            }
        }

        private void findDiagonalFightingPairs(BattalionInfo me, int rowId, RefRW<DataHolder> dataHolder)
        {
            var positions = dataHolder.ValueRO.positions;
            foreach (var bellow in positions.GetValuesForKey(rowId - 1))
            {
                var isTooFarDiagonal = BattleTransformUtils.isTooFar(me.position, bellow.position, me.width, bellow.width, 0.5f);
                //skip same team
                if (me.team == bellow.team)
                {
                    continue;
                }

                if (!isTooFarDiagonal)
                {
                    addFightingPair(me.battalionId, bellow.battalionId, BattalionFightType.VERTICAL, dataHolder);
                }
            }
        }

        private void addFightingPair(long battalionId1, long battalionId2, BattalionFightType fightType, RefRW<DataHolder> dataHolder)
        {
            dataHolder.ValueRW.fightingPairs.Add(new FightingPair
            {
                battalionId1 = battalionId1,
                battalionId2 = battalionId2,
                fightType = fightType
            });
            dataHolder.ValueRW.battalionsPerformingAction.Add(battalionId1);
            dataHolder.ValueRW.battalionsPerformingAction.Add(battalionId2);
            dataHolder.ValueRW.fightingBattalions.Add(battalionId1);
            dataHolder.ValueRW.fightingBattalions.Add(battalionId2);
        }
    }
}