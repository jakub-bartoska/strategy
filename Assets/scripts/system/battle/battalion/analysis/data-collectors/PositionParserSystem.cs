using System.Collections.Generic;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

            var tmpUnsortedData = new NativeParallelMultiHashMap<int, BattalionInfo>(1000, Allocator.TempJob);
            new CollectBattleUnitPositionsJob
                {
                    battalionPositions = tmpUnsortedData
                }.Schedule(state.Dependency)
                .Complete();

            var sorter = new SortByPosition();
            var allRows = dataHolder.ValueRO.allRowIds;

            foreach (var row in allRows)
            {
                var unsortedRowData = new NativeList<BattalionInfo>(100, Allocator.Temp);
                foreach (var value in tmpUnsortedData.GetValuesForKey(row))
                {
                    unsortedRowData.Add(value);
                    if (value.unitType == BattleUnitTypeEnum.BATTALION)
                    {
                        dataHolder.ValueRW.battalionInfo.Add(value.battalionId, value);
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

        public class SortByPosition : IComparer<BattalionInfo>
        {
            public int Compare(BattalionInfo e1, BattalionInfo e2)
            {
                return e2.position.x.CompareTo(e1.position.x);
            }
        }

        [BurstCompile]
        public partial struct CollectBattleUnitPositionsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, BattalionInfo> battalionPositions;

            private void Execute(BattleUnitType battleUnitType, LocalTransform transform, Row row, BattalionTeam team, BattalionWidth width)
            {
                battalionPositions.Add(row.value, new BattalionInfo
                {
                    battalionId = battleUnitType.id,
                    position = transform.Position,
                    team = team.value,
                    width = width.value,
                    unitType = battleUnitType.type
                });
            }
        }
    }
}