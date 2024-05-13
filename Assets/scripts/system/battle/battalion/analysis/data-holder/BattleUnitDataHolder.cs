using component;
using component.battle.battalion;
using system.battle.enums;
using Unity.Collections;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.data_holder
{
    public class BattleUnitDataHolder
    {
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
         * battalionId - (blockerId, blockerType)
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
    }
}