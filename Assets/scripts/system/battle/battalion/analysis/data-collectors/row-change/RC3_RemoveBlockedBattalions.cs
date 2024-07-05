using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.enums;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.row_change
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(RC2_MarkRowChangeBattalions))]
    public partial struct RC3_RemoveBlockedBattalions : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();

            var battalionSwitchRowDirectionsCopy = getBattalionSwitchRowDirectionsCopy(dataHolder.ValueRO);
            var blockers = movementDataHolder.ValueRO.blockers;

            foreach (var battalionSwitchRowDirection in battalionSwitchRowDirectionsCopy)
            {
                foreach (var blocker in blockers.GetValuesForKey(battalionSwitchRowDirection.Key))
                {
                    if (blocker.blockerType == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    if (blocker.blockingDirection != battalionSwitchRowDirection.Value)
                    {
                        continue;
                    }

                    dataHolder.ValueRO.battalionSwitchRowDirections.Remove(battalionSwitchRowDirection.Key);
                }
            }

            var waitingForSoldiersBattalions = movementDataHolder.ValueRO.waitingForSoldiersBattalions;
            foreach (var waitingForSoldiersBattalion in waitingForSoldiersBattalions)
            {
                dataHolder.ValueRW.battalionSwitchRowDirections.Remove(waitingForSoldiersBattalion);
            }
        }

        private NativeHashMap<long, Direction> getBattalionSwitchRowDirectionsCopy(DataHolder dataHolder)
        {
            var battalionSwitchRowDirections = dataHolder.battalionSwitchRowDirections;
            var result = new NativeHashMap<long, Direction>(battalionSwitchRowDirections.Count, Allocator.Temp);
            foreach (var record in battalionSwitchRowDirections)
            {
                result.Add(record.Key, record.Value);
            }

            return result;
        }
    }
}