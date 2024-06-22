using system.battle.enums;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.battle.battalion.data_holders
{
    public struct DataHolder : IComponentData
    {
        /*
         * All alive battalionIds
         */
        public NativeHashSet<long> allBattalionIds;

        /*
         * contains ids of all rows (even empty ones)
         */
        public NativeList<int> allRowIds;

        /**
         * row - (battalionId, position, team, width, unitType)
         * sorted by row from 0 to max
         * sorted within row from left to right
         */
        public NativeParallelMultiHashMap<int, BattalionInfo> positions;

        /**
         * battalionId - (battalionId, position, team, width, unitType)
         */
        public NativeHashMap<long, BattalionInfo> battalionInfo;

        /**
         * (battalionId, battalionId, BattalionFightType)
         * 2 battalions fighting each other + type of the fight
         * each fighting pair should be added only once
         */
        public NativeList<FightingPair> fightingPairs;

        /**
         * battalion id
         * Battalions which are currently in any form of fight
         */
        public NativeHashSet<long> fightingBattalions;

        /**
         * battalionId
         * battalions which are not moving
         */
        public NativeHashSet<long> battalionsPerformingAction;

        /**
         * battalionId - missing soldier position within battalion
         * If battalion with ID 99 has max 10 soldiers and has 2 death on positions 4 and 10, map will contain following records:
         * 99 - 4
         * 99 - 10
         */
        public NativeParallelMultiHashMap<long, int> needReinforcements;

        /**
         * battalion which should receive reinforcements - reinforcement soldier
         * list contains soldiers which are moving from old battalion to new one (new one is key in this map)
         * 1 battalion can receive multiple reinforcements
         */
        public NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements;

        /**
         * battalion id
         * Battalion ids which are flanking
         */
        public NativeHashSet<long> flankingBattalions;

        /**
         * rowId - (team1 row change direction - closest enemy row, team2 row change direction - closest enemy row)
         *  - if Team 2 has any unit in row 3, value for team1 direction will be NONE - 3
         *  - if them 2 has 0 units in row 3, 1 in row 5 and 1 in row 2, direction for team 1 for row 3 will be up-3 since it is the most close enemy row
         */
        public NativeHashMap<int, RowChange> rowChanges;

        /**
         * battalionId - direction in which battalion should switch row
         * does not contain all battalions, jsut battalions which should switch row
         * can contain only directions up or down
         */
        public NativeHashMap<long, Direction> battalionSwitchRowDirections;

        /**
         * battalionID - direction for not allowed horizontal split
         * - list can contain only directions LEFT and RIGHT
         * - lists all battalions which can not split horizontally
         */
        public NativeParallelMultiHashMap<long, Direction> blockedHorizontalSplits;

        /**
         * battalionId - direction of split
         * contains only battalions which should split + direction of split.
         * direction is final, so 1 battalion can contain only 1 record
         */
        public NativeHashMap<long, Direction> splitBattalions;
    }

    public struct BattalionInfo
    {
        public long battalionId;
        public float3 position;
        public Team team;
        public float width;
        public BattleUnitTypeEnum unitType;
    }

    public struct FightingPair
    {
        public long battalionId1;
        public long battalionId2;
        public BattalionFightType fightType;
    }

    public struct RowChange
    {
        public TeamRowChange team1;
        public TeamRowChange team2;
    }

    public struct TeamRowChange
    {
        public Direction direction;
        public int closestEnemyRow;
    }
}