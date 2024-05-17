using component._common.system_switchers;
using component.battle.battalion;
using system.battle.battalion.analysis.data_holder;
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
            var reinforcements = DataHolder.reinforcements;

            new ReceiveReinforcementsJob
                {
                    reinforcements = reinforcements
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct ReceiveReinforcementsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<long, BattalionSoldiers> reinforcements;

            private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionSoldiers> soldiers, ref BattalionHealth health)
            {
                if (!reinforcements.ContainsKey(battalionMarker.id))
                {
                    return;
                }

                var healthIncrease = 0;
                foreach (var soldier in reinforcements.GetValuesForKey(battalionMarker.id))
                {
                    healthIncrease += 10;
                    soldiers.Add(soldier);
                }

                health.value += healthIncrease;
            }
        }
    }
}