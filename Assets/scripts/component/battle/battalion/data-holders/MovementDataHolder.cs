using system.battle.enums;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.battle.battalion.data_holders
{
    public struct MovementDataHolder : IComponentData
    {
        /**
         * blocked battalionId - (blockerId, blockerType)
         * keep in mind that battalion id = shadow id from this battalion. So When shadow is blocked, it is added as battalion
         */
        public NativeParallelMultiHashMap<long, (long, BattleUnitTypeEnum, Direction, Team)> blockers;

        /**
         * blocker battalion id - (battalion which is blocked, direction)
         * direction - the same direction as from "blockers map"
         * A -> B Right in blockers
         * B (A, Right) in this map
         */
        public NativeParallelMultiHashMap<long, (long, Direction)> battalionFollowers;

        /**
         * battalionId - direction battalion should move in normal conditions
         */
        public NativeHashMap<long, Direction> battalionDefaultMovementDirection;

        /**
         * rowId - (team1FlankPosition, team2FlankPosition)
         * flank position for team1 = when team1 is on right side of this position, it should go left instead of right
         * flank position for team2 = when team2 is on left side of this position, it should go right instead of left
         *
         * positions are null in case that row contains only other team units
         */
        public NativeHashMap<int, (float3?, float3?)> flankPositions;

        /**
         * battalionId - (direction, distance, mindDistanceEnemyId)
         * battalionId - (direction, distance, mindDistanceEnemyId)
         * contains only battalions which should move in exact direction
         * X axis distance from closest enemy
         * distance is always positive number (even when enemy is behind)
         * id of enemy which is causing direction change
         */
        public NativeHashMap<long, (Direction, float, long)> inFightMovement;

        /**
         * battalionId - direction
         * battalion planned directions, contains even battalions which are stopped.
         * useful for reinforcements
         */
        public NativeHashMap<long, Direction> plannedMovementDirections;

        /**
         * battalionId - direction
         * contains only battalions which are moving
         */
        public NativeHashMap<long, Direction> movingBattalions;

        /**
         * battalionId - distance
         * contains only battalions which should move in exact distance
         */
        public NativeHashMap<long, float> battalionExactDistance;

        /**
         * battalionId
         * contains newly created battalions which were just created and are waiting till their soldiers arrive
         */
        public NativeHashSet<long> waitingForSoldiersBattalions;
    }
}