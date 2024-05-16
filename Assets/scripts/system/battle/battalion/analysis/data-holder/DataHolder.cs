using component;
using component.battle.battalion;
using system.battle.enums;
using Unity.Collections;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.data_holder
{
    public class DataHolder
    {
        /*
         * All alive battalionIds
         */
        public static NativeHashSet<long> allBattalionIds = new(1000, Allocator.Persistent);

        /*
         * contains ids of all rows (even empty ones)
         */
        public static NativeList<int> allRowIds = new(10, Allocator.Persistent);

        /**
         * row - (battalionId, position, team, width)
         * sorted by row from 0 to max
         * sorted within row from left to right
         */
        public static NativeParallelMultiHashMap<int, (long, float3, Team, float, BattleUnitTypeEnum)> positions = new(1000, Allocator.Persistent);

        /**
         * blocked battalionId - (blockerId, blockerType)
         * keep in mind that battalion id = shadow id from this battalion. So When shadow is blocked, it is added as battalion
         */
        public static NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction)> blockers = new(1000, Allocator.Persistent);

        /**
         * blocker battalion id - (battalion which is blocked, direction)
         * direction - the same direction as from "blockers map"
         * A -> B Right in blockers
         * B (A, Right) in this map
         */
        public static NativeParallelMultiHashMap<long, (long, Direction)> battalionFollowers = new(1000, Allocator.Persistent);

        /**
         * battalionId - direction battalion should move in normal conditions
         */
        public static NativeHashMap<long, Direction> battalionDefaultMovementDirection = new(1000, Allocator.Persistent);

        /**
         * (battalionId, battalionId, BattalionFightType)
         * 2 battalions fighting each other + type of the fight
         */
        public static NativeList<(long, long, BattalionFightType)> fightingPairs = new(1000, Allocator.Persistent);

        /**
         * battalionId
         * battalions which are not moving
         */
        public static NativeHashSet<long> notMovingBattalions = new(1000, Allocator.Persistent);

        /**
         * battalionId - missing soldier position within battalion
         * If battalion with ID 99 has max 10 soldiers and has 2 death on positions 4 and 10, map will contain following records:
         * 99 - 4
         * 99 - 10
         */
        public static NativeParallelMultiHashMap<long, int> needReinforcements = new(1000, Allocator.Persistent);
    }
}