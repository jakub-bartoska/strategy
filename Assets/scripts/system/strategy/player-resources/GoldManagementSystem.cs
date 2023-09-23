using _Monobehaviors.ui.player_resources;
using component;
using component._common.system_switchers;
using component.strategy.general;
using component.strategy.player_resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.player_resources
{
    public partial struct GoldManagementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<ResourceHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var resourceHolder = SystemAPI.GetSingletonBuffer<ResourceHolder>();

            new ProcessResourceGenerators
                {
                    deltaTime = deltaTime,
                    resources = resourceHolder
                }.Schedule(state.Dependency)
                .Complete();

            foreach (var holder in resourceHolder)
            {
                ResourcesUi.instance.updateResource(holder.type, holder.value);
            }
        }

        [BurstCompile]
        public partial struct ProcessResourceGenerators : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            public DynamicBuffer<ResourceHolder> resources;

            private void Execute(ref DynamicBuffer<ResourceGenerator> resourceGenerators, TeamComponent team)
            {
                if (team.team != Team.TEAM1) return;

                for (var i = 0; i < resourceGenerators.Length; i++)
                {
                    var resource = resourceGenerators[i];
                    resource.timeRemaining -= deltaTime;
                    if (resource.timeRemaining < 0)
                    {
                        resource.timeRemaining += resourceGenerators[i].defaultTimer;
                        for (int j = 0; j < resources.Length; j++)
                        {
                            if (resources[j].type != resource.type) continue;
                            var newResourceHolder = resources[j];
                            newResourceHolder.value += resource.value;
                            resources[j] = newResourceHolder;
                        }
                    }

                    resourceGenerators[i] = resource;
                }
            }
        }
    }
}