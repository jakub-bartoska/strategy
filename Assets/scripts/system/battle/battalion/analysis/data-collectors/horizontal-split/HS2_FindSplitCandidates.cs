using System;
using component._common.system_switchers;
using system.battle.battalion.analysis.data_holder;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.horizontal_split
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(HS1_FindHorizontalSplitBlockers))]
    public partial struct HS2_FindSplitCandidates : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var result = DataHolder.splitBattalions;
            //containns battalions which fight vertically only
            var verticalFighters = getVerticalFighters();
            removeBlockedBattalions(verticalFighters);

            var battalionDefaultMovementDirection = DataHolder.battalionDefaultMovementDirection;
            foreach (var verticalFighter in verticalFighters)
            {
                battalionDefaultMovementDirection.TryGetValue(verticalFighter, out var defaultDirection);
                result.Add(verticalFighter, defaultDirection);
            }
        }

        private NativeHashSet<long> getVerticalFighters()
        {
            var fightingPairs = DataHolder.fightingPairs;
            //UP/DOWN
            var verticalFights = new NativeHashSet<long>(1000, Allocator.Temp);
            //LEFT/RIGHT
            var normalFights = new NativeHashSet<long>(1000, Allocator.Temp);

            foreach (var fightingPair in fightingPairs)
            {
                switch (fightingPair.Item3)
                {
                    case BattalionFightType.NORMAL:
                        normalFights.Add(fightingPair.Item1);
                        normalFights.Add(fightingPair.Item2);
                        break;
                    case BattalionFightType.VERTICAL:
                        verticalFights.Add(fightingPair.Item1);
                        verticalFights.Add(fightingPair.Item2);
                        break;
                    default:
                        throw new Exception("Unknown fight type");
                }
            }

            foreach (var normalFightBattalionId in normalFights)
            {
                verticalFights.Remove(normalFightBattalionId);
            }

            return verticalFights;
        }

        private void removeBlockedBattalions(NativeHashSet<long> fightingBattalionIds)
        {
            var battalionDefaultMovementDirection = DataHolder.battalionDefaultMovementDirection;
            var blockedHorizontalSplits = DataHolder.blockedHorizontalSplits;

            var blockedBattalions = new NativeHashSet<long>(1000, Allocator.Temp);
            foreach (var fightingBattalionId in fightingBattalionIds)
            {
                battalionDefaultMovementDirection.TryGetValue(fightingBattalionId, out var defaultDirection);
                foreach (var direction in blockedHorizontalSplits.GetValuesForKey(fightingBattalionId))
                {
                    if (direction == defaultDirection)
                    {
                        blockedBattalions.Add(fightingBattalionId);
                    }
                }
            }

            foreach (var blockedBattalion in blockedBattalions)
            {
                fightingBattalionIds.Remove(blockedBattalion);
            }
        }
    }
}