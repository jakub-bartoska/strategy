﻿using component;
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
         * battalionId - (position, team, width)
         */
        public static NativeHashMap<long, (float3, Team, float)> battalionInfo = new(1000, Allocator.Persistent);

        /**
         * (battalionId, battalionId, BattalionFightType)
         * 2 battalions fighting each other + type of the fight
         * each fighting pair should be added only once
         */
        public static NativeList<(long, long, BattalionFightType)> fightingPairs = new(1000, Allocator.Persistent);

        /**
         * battalion id
         * Battalions which are currently in any form of fight
         */
        public static NativeHashSet<long> fightingBattalions = new(1000, Allocator.Persistent);

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
         * battalion which should receive reinforcements - reinforcement soldier
         * list contains soldiers which are moving from old battalion to new one (new one is key in this map)
         * 1 battalion can receive multiple reinforcements
         */
        public static NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements = new(1000, Allocator.Persistent);

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

        /**
         * battalionID - direction for not allowed horizontal split
         * - list can contain only directions LEFT and RIGHT
         * - lists all battalions which can not split horizontally
         */
        public static NativeParallelMultiHashMap<long, Direction> blockedHorizontalSplits = new(1000, Allocator.Persistent);

        /**
         * battalionId - direction of split
         * contains only battalions which should split + direction of split.
         * direction is final, so 1 battalion can contain only 1 record
         */
        public static NativeHashMap<long, Direction> splitBattalions = new(1000, Allocator.Persistent);
    }
}