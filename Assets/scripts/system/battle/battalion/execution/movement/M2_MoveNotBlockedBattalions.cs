using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
using system.battle.battalion.execution.movement;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(MD3_AdjustByBattleMovements))]
    public partial struct M2_MoveNotBlockedBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blockers = MovementDataHolder.blockers;

            //battalionID -> direction to move
            var battalionsAbleToMove = new NativeList<(long, Direction)>(1000, Allocator.Temp);
            var stoppedBattalions = getNotMovingBattalionIds();
            var allBattalionIds = DataHolder.allBattalionIds;

            foreach (var battalionId in allBattalionIds)
            {
                if (stoppedBattalions.Contains(battalionId))
                {
                    continue;
                }

                var blockedForDirection = false;
                var direction = MovementDataHolder.plannedMovementDirections[battalionId];
                foreach (var valueTuple in blockers.GetValuesForKey(battalionId))
                {
                    if (valueTuple.Item3 == direction)
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

            var ableToMoveInDefaultDirection = BlockerUtils.unblockDirections(battalionsAbleToMove);
            foreach (var valueTuple in battalionsAbleToMove)
            {
                ableToMoveInDefaultDirection.Add(valueTuple.Item1, valueTuple.Item2);
            }

            foreach (var battalion in ableToMoveInDefaultDirection)
            {
                MovementDataHolder.movingBattalions.Add(battalion.Key, battalion.Value);
            }
        }

        private NativeHashSet<long> getNotMovingBattalionIds()
        {
            var result = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var inActionBattalion in DataHolder.battalionsPerformingAction)
            {
                result.Add(inActionBattalion);
            }

            foreach (var fightAndMoveBattalions in MovementDataHolder.inFightMovement.GetKeyArray(Allocator.Temp))
            {
                result.Remove(fightAndMoveBattalions);
            }

            return result;
        }
    }
}