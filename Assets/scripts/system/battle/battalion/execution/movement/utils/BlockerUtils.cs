using component.battle.battalion.data_holders;
using system.battle.enums;
using Unity.Collections;

namespace system.battle.battalion.execution
{
    public class BlockerUtils
    {
        /**
         * battalionID - directions to remove from blockers
         */
        public static NativeHashMap<long, Direction> unblockDirections(NativeList<(long, Direction)> toUnblock, MovementDataHolder movementDataHolder)
        {
            var result = new NativeHashMap<long, Direction>(1000, Allocator.Temp);
            foreach (var battalionDirection in toUnblock)
            {
                unblockFollowers(result, battalionDirection.Item1, battalionDirection.Item2, movementDataHolder);
            }

            return result;
        }

        private static void unblockFollowers(NativeHashMap<long, Direction> result, long battalionId, Direction direction, MovementDataHolder movementDataHolder)
        {
            var followers = movementDataHolder.battalionFollowers;
            if (followers.ContainsKey(battalionId))
            {
                foreach (var follower in followers.GetValuesForKey(battalionId))
                {
                    if (follower.Item2 != direction)
                    {
                        continue;
                    }

                    if (!isBlockedByAnotherBattalion(result, follower.Item1, follower.Item2, movementDataHolder))
                    {
                        result.Add(follower.Item1, follower.Item2);
                        unblockFollowers(result, follower.Item1, follower.Item2, movementDataHolder);
                    }
                }
            }
        }

        private static bool isBlockedByAnotherBattalion(NativeHashMap<long, Direction> result, long battalionId, Direction direction, MovementDataHolder movementDataHolder)
        {
            foreach (var blocked in movementDataHolder.blockers.GetValuesForKey(battalionId))
            {
                if (blocked.Item3 != direction)
                {
                    continue;
                }

                if (!result.ContainsKey(blocked.Item1))
                {
                    return true;
                }
            }

            return false;
        }
    }
}