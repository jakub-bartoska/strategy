using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.analysis.utils;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

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
                BattalionInfo? leftUnitOptional = null;

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

                    var isTooFar = BattleTransformUtils.isTooFar(me.position, leftUnit.position, me.width, leftUnit.width);
                    if (!isTooFar)
                    {
                        addBlocker(me, leftUnit, blockers);
                    }
                }
            }

            createFollowers(movementDataHolder);
        }

        private void findDiagonalBlockers(
            BattalionInfo me,
            int rowId,
            NativeParallelMultiHashMap<long, BattalionBlocker> blockers,
            DataHolder dataHolder)
        {
            var positions = dataHolder.positions;
            foreach (var upper in positions.GetValuesForKey(rowId - 1))
            {
                var isTooFarDiagonal = BattleTransformUtils.isTooFar(me.position, upper.position, me.width, upper.width);

                if (!isTooFarDiagonal)
                {
                    addBlockerVertical(me, upper, blockers);
                }
            }
        }

        private void addBlocker(BattalionInfo right, BattalionInfo left,
            NativeParallelMultiHashMap<long, BattalionBlocker> blockers)
        {
            //left to right
            blockers.Add(left.battalionId, new BattalionBlocker
            {
                blockerId = right.battalionId,
                blockingDirection = Direction.RIGHT,
                blockerType = right.unitType,
                team = right.team
            });
            //right to left
            blockers.Add(right.battalionId, new BattalionBlocker
            {
                blockerId = left.battalionId,
                blockingDirection = Direction.LEFT,
                blockerType = left.unitType,
                team = left.team
            });
        }

        private void addBlockerVertical(BattalionInfo bottom, BattalionInfo upper,
            NativeParallelMultiHashMap<long, BattalionBlocker> blockers)
        {
            //upper to down
            blockers.Add(upper.battalionId, new BattalionBlocker
            {
                blockerId = bottom.battalionId,
                blockingDirection = Direction.DOWN,
                blockerType = bottom.unitType,
                team = bottom.team
            });
            //down to top
            blockers.Add(bottom.battalionId, new BattalionBlocker
            {
                blockerId = upper.battalionId,
                blockingDirection = Direction.UP,
                blockerType = upper.unitType,
                team = upper.team
            });
        }


        private void createFollowers(RefRW<MovementDataHolder> movementDataHolder)
        {
            var blockers = movementDataHolder.ValueRO.blockers;
            var battalionFollowers = movementDataHolder.ValueRW.battalionFollowers;

            foreach (var blocked in blockers)
            {
                battalionFollowers.Add(blocked.Value.blockerId, new BattalionFollower
                {
                    blockedBattalionId = blocked.Key,
                    direction = blocked.Value.blockingDirection
                });
            }
        }
    }
}