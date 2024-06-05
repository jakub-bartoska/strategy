using component;
using component.battle.battalion;
using system.battle.enums;
using Unity.Collections;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.data_holder.movement
{
    public class MovementDataHolder
    {
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
         * rowId - (team1FlankPosition, team2FlankPosition)
         * flank position for team1 = when team1 is on right side of this position, it should go left instead of right
         * flank position for team2 = when team2 is on left side of this position, it should go right instead of left
         *
         * positions are null in case that row contains only other team units
         */
        public static NativeHashMap<int, (float3?, float3?)> flankPositions = new(10, Allocator.Persistent);

        /**
         * battalionId - (direction, distance)
         * battalionId - (direction, distance)
         * contains only battalions which should move in exact direction
         * X axis distance from closest enemy
         */
        public static NativeHashMap<long, (Direction, float)> inFightMovement = new(1000, Allocator.Persistent);

        /**
         * battalionId - direction
         * battalion planned directions, contains even battalions which are stopped.
         * useful for reinforcements
         */
        public static NativeHashMap<long, Direction> plannedMovementDirections = new(1000, Allocator.Persistent);

        /**
         * battalionId - direction
         * contains only battalions which are moving
         */
        public static NativeHashMap<long, Direction> movingBattalions = new(1000, Allocator.Persistent);
    }
}