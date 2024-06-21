﻿using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.analysis
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    public partial struct PositionParserSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        /**
         * Fetch all battalion and shadow positions, sort them by row -> sort by x position within row -> save data
         */
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var positions = dataHolder.ValueRO.positions;

            var tmpUnsortedData = new NativeParallelMultiHashMap<int, (long, float3, Team, float, BattleUnitTypeEnum)>(1000, Allocator.TempJob);
            new CollectBattleUnitPositionsJob
                {
                    battalionPositions = tmpUnsortedData
                }.Schedule(state.Dependency)
                .Complete();

            var sorter = new SortByPosition();
            var allRows = dataHolder.ValueRO.allRowIds;

            foreach (var row in allRows)
            {
                var unsortedRowData = new NativeList<(long, float3, Team, float, BattleUnitTypeEnum)>(100, Allocator.Temp);
                foreach (var value in tmpUnsortedData.GetValuesForKey(row))
                {
                    unsortedRowData.Add(value);
                    if (value.Item5 == BattleUnitTypeEnum.BATTALION)
                    {
                        dataHolder.ValueRW.battalionInfo.Add(value.Item1, (value.Item2, value.Item3, value.Item4));
                    }
                }

                if (unsortedRowData.IsEmpty)
                {
                    continue;
                }

                unsortedRowData.Sort(sorter);
                foreach (var value in unsortedRowData)
                {
                    positions.Add(row, value);
                }
            }

            tmpUnsortedData.Dispose();
        }

        public class SortByPosition : IComparer<(long, float3, Team, float, BattleUnitTypeEnum)>
        {
            public int Compare((long, float3, Team, float, BattleUnitTypeEnum) e1, (long, float3, Team, float, BattleUnitTypeEnum) e2)
            {
                return e2.Item2.x.CompareTo(e1.Item2.x);
            }
        }

        [BurstCompile]
        public partial struct CollectBattleUnitPositionsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, (long, float3, Team, float, BattleUnitTypeEnum)> battalionPositions;

            private void Execute(BattleUnitType battleUnitType, LocalTransform transform, Row row, BattalionTeam team, BattalionWidth width)
            {
                battalionPositions.Add(row.value, (battleUnitType.id, transform.Position, team.value, width.value, battleUnitType.type));
            }
        }
    }
}