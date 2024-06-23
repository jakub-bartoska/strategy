using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.execution.reinforcement
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(R2_SendReinforcementsSystem))]
    public partial struct R3_ReceiveReinforcementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var reinforcements = dataHolder.ValueRO.reinforcements;

            new ReceiveReinforcementsJob
                {
                    reinforcements = reinforcements
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct ReceiveReinforcementsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, Reinforcements> reinforcements;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionSoldiers> soldiers, ref BattalionHealth health)
            {
                if (!reinforcements.ContainsKey(battalionMarker.id))
                {
                    return;
                }

                var existingPositions = getExistingIndexes(soldiers);

                var healthIncrease = 0;
                foreach (var soldier in reinforcements.GetValuesForKey(battalionMarker.id))
                {
                    //if reinforcement have index within battalion E.G. 1, and battalion have already soldier on position 1, reinforcement has to go to different position within battalion
                    //if index within battalion is free, soldier is unchanged
                    var updatedSoldier = updatePositionWithinBattalion(ref existingPositions, soldier);

                    healthIncrease += 10;

                    soldiers.Add(updatedSoldier);
                }

                health.value += healthIncrease;
            }

            private BattalionSoldiers updatePositionWithinBattalion(ref NativeHashSet<int> existingPositions, Reinforcements reinforcement)
            {
                if (!existingPositions.Contains(reinforcement.reinforcement.positionWithinBattalion))
                {
                    return reinforcement.reinforcement;
                }

                var emptyIndex = getFirstEmptyPosition(ref existingPositions);
                existingPositions.Add(emptyIndex);
                reinforcement.reinforcement.positionWithinBattalion = emptyIndex;
                return new BattalionSoldiers
                {
                    positionWithinBattalion = emptyIndex,
                    entity = reinforcement.reinforcement.entity,
                    soldierId = reinforcement.reinforcement.soldierId
                };
            }

            private int getFirstEmptyPosition(ref NativeHashSet<int> existingPositions)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (!existingPositions.Contains(i))
                    {
                        return i;
                    }
                }

                throw new Exception("unable to receive reinforcements, no empty position in battalion");
            }

            private NativeHashSet<int> getExistingIndexes(DynamicBuffer<BattalionSoldiers> soldiers)
            {
                var existingPositions = new NativeHashSet<int>(10, Allocator.Temp);
                foreach (var soldier in soldiers)
                {
                    existingPositions.Add(soldier.positionWithinBattalion);
                }

                return existingPositions;
            }
        }
    }
}