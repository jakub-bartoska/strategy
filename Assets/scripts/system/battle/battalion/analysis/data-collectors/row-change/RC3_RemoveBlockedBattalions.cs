using component._common.system_switchers;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.data_holder.movement;
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
            var battalionSwitchRowDirectionsCopy = getBattalionSwitchRowDirectionsCopy();
            var blockers = MovementDataHolder.blockers;

            foreach (var battalionSwitchRowDirection in battalionSwitchRowDirectionsCopy)
            {
                foreach (var (id, type, direction, team) in blockers.GetValuesForKey(battalionSwitchRowDirection.Key))
                {
                    if (type == BattleUnitTypeEnum.SHADOW)
                    {
                        continue;
                    }

                    if (direction != battalionSwitchRowDirection.Value)
                    {
                        continue;
                    }

                    DataHolder.battalionSwitchRowDirections.Remove(battalionSwitchRowDirection.Key);
                }
            }

            var waitingForSoldiersBattalions = MovementDataHolder.waitingForSoldiersBattalions;
            foreach (var waitingForSoldiersBattalion in waitingForSoldiersBattalions)
            {
                DataHolder.battalionSwitchRowDirections.Remove(waitingForSoldiersBattalion);
            }
        }

        private NativeHashMap<long, Direction> getBattalionSwitchRowDirectionsCopy()
        {
            var battalionSwitchRowDirections = DataHolder.battalionSwitchRowDirections;
            var result = new NativeHashMap<long, Direction>(battalionSwitchRowDirections.Count, Allocator.Temp);
            foreach (var record in battalionSwitchRowDirections)
            {
                result.Add(record.Key, record.Value);
            }

            return result;
        }
    }
}