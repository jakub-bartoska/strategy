using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using Unity.Collections;

namespace system.battle.battalion.execution
{
    public class BlockerUtils
    {
        /**
         * battalionID - directions to remove from blockers
         */
        public static NativeList<long> unblockDirections(NativeList<(long, Direction)> toUnblock)
        {
            var result = new NativeList<long>(Allocator.TempJob);
            var followers = BattleUnitDataHolder.battalionFollowers;
            foreach (var battalionDirection in toUnblock)
            {
                unblockFollowers(result, battalionDirection.Item1, battalionDirection.Item2);
            }

            return result;
        }

        private static void unblockFollowers(NativeList<long> result, long battalionId, Direction direction)
        {
            var followers = BattleUnitDataHolder.battalionFollowers;
            if (followers.ContainsKey(battalionId))
            {
                foreach (var follower in followers.GetValuesForKey(battalionId))
                {
                    if (follower.Item2 != direction)
                    {
                        continue;
                    }

                    if (!isBlockedByAnotherBattalion(result, follower.Item1, follower.Item2))
                    {
                        result.Add(follower.Item1);
                        unblockFollowers(result, follower.Item1, follower.Item2);
                    }
                }
            }
        }

        private static bool isBlockedByAnotherBattalion(NativeList<long> result, long battalionId, Direction direction)
        {
            foreach (var blocked in BattleUnitDataHolder.blockers.GetValuesForKey(battalionId))
            {
                if (blocked.Item3 != direction)
                {
                    continue;
                }

                if (!result.Contains(blocked.Item1))
                {
                    return true;
                }
            }

            return false;
        }
    }
}