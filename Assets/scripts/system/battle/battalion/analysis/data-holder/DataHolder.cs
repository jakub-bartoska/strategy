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
        public static NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers = new(1000, Allocator.Persistent);

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
        public static NativeHashSet<long> battalionsPerformingAction = new(1000, Allocator.Persistent);

        /**
         * battalionId - missing soldier position within battalion
         * If battalion with ID 99 has max 10 soldiers and has 2 death on positions 4 and 10, map will contain following records:
         * 99 - 4
         * 99 - 10
         */
        public static NativeParallelMultiHashMap<long, int> needReinforcements = new(1000, Allocator.Persistent);

        /**
         * battalion which should add reinforcements - reinforcement soldier
         * 1 battalion can receive multiple reinforcements
         */
        public static NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements = new(1000, Allocator.Persistent);

        /**
         * rowId - (team1FlankPosition, team2FlankPosition)
         * flank position for team1 = when team1 is on right side of this position, it should go left instead of right
         * flank position for team2 = when team2 is on left side of this position, it should go right instead of left
         *
         * positions are null in case that row contains only other team units
         */
        public static NativeHashMap<int, (float3?, float3?)> flankPositions = new(10, Allocator.Persistent);

        /**
         * battalion id
         * Battalion ids which are flanking
         */
        public static NativeHashSet<long> flankingBattalions = new(1000, Allocator.Persistent);

        /**
         * rowId - (team1 row change direction - closest enemy row, team2 row change direction - closest enemy row)
         *  - if Team 2 has any unit in row 3, value for team1 direction will be NONE - 3
         *  - if them 2 has 0 units in row 3, 1 in row 5 and 1 in row 2, direction for team 1 for row 3 will be up-3 since it is the most close enemy row
         */
        public static NativeHashMap<int, ((Direction, int), (Direction, int))> rowChanges = new(10, Allocator.Persistent);

        /**
         * battalionId - direction in which battalion should switch row
         * does not contain all battalions, jsut battalions which should switch row
         * can contain only directions up or down
         */
        public static NativeHashMap<long, Direction> battalionSwitchRowDirections = new(1000, Allocator.Persistent);
    }
}