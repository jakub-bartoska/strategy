using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.ui;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace system.strategy.ui
{
    public partial struct ArmyLabelSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new UpdateUiLabelJob()
                .ScheduleParallel(state.Dependency)
                .Complete();
        }
    }

    public partial struct UpdateUiLabelJob : IJobEntity
    {
        private void Execute(ref StrategyUiLabel label, ArmyTag tag, LocalTransform transform,
            DynamicBuffer<ArmyCompany> companies)
        {
            var soldierCount = 0;
            foreach (var armyCompany in companies)
            {
                soldierCount += armyCompany.soldierCount;
            }

            label.position = transform.Position;
            label.text = soldierCount.ToString();
        }
    }
}