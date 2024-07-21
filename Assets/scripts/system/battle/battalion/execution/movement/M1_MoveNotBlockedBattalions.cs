using component._common.system_switchers;
using component.battle.battalion.data_holders;
using system.battle.battalion.row_change;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(RC3_MoveBetweenRows))]
    public partial struct M1_MoveNotBlockedBattalions : ISystem
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

            var blockers = movementDataHolder.ValueRO.blockers;

            //battalionID -> direction to move
            var battalionsAbleToMove = new NativeList<(long, Direction)>(1000, Allocator.Temp);
            var stoppedBattalions = getNotMovingBattalionIds(dataHolder.ValueRO, movementDataHolder.ValueRO);
            var allBattalionIds = dataHolder.ValueRO.allBattalionIds;

            foreach (var battalionId in allBattalionIds)
            {
                if (stoppedBattalions.Contains(battalionId))
                {
                    continue;
                }

                var blockedForDirection = false;
                var direction = movementDataHolder.ValueRO.plannedMovementDirections[battalionId];
                foreach (var valueTuple in blockers.GetValuesForKey(battalionId))
                {
                    if (valueTuple.blockingDirection == direction)
                    {
                        blockedForDirection = true;
                        break;
                    }
                }

                if (!blockedForDirection)
                {
                    battalionsAbleToMove.Add((battalionId, direction));
                }
            }

            var ableToMoveInDefaultDirection = BlockerUtils.unblockDirections(battalionsAbleToMove, movementDataHolder.ValueRO);
            foreach (var valueTuple in battalionsAbleToMove)
            {
                ableToMoveInDefaultDirection.Add(valueTuple.Item1, valueTuple.Item2);
            }

            foreach (var battalion in ableToMoveInDefaultDirection)
            {
                movementDataHolder.ValueRW.movingBattalions.Add(battalion.Key, battalion.Value);
            }
        }

        private NativeHashSet<long> getNotMovingBattalionIds(DataHolder dataholde, MovementDataHolder movementDataHolder)
        {
            var result = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var inActionBattalion in dataholde.battalionsPerformingAction)
            {
                result.Add(inActionBattalion);
            }

            foreach (var fightAndMoveBattalions in movementDataHolder.inFightMovement.GetKeyArray(Allocator.Temp))
            {
                result.Remove(fightAndMoveBattalions);
            }

            return result;
        }
    }
}