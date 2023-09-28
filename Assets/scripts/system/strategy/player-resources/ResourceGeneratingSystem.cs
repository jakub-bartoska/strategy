using component._common.system_switchers;
using component.strategy.player_resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.strategy.player_resources
{
    public partial struct ResourceGeneratingSystem : ISystem
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
            new ProcessResourceGenerators
                {
                    deltaTime = deltaTime,
                }.Schedule(state.Dependency)
                .Complete();
        }

        [BurstCompile]
        public partial struct ProcessResourceGenerators : IJobEntity
        {
            [ReadOnly] public float deltaTime;

            private void Execute(ref DynamicBuffer<ResourceGenerator> resourceGenerators, ref DynamicBuffer<ResourceHolder> resources)
            {
                for (var i = 0; i < resourceGenerators.Length; i++)
                {
                    var resource = resourceGenerators[i];
                    resource.timeRemaining -= deltaTime;
                    if (resource.timeRemaining < 0)
                    {
                        resource.timeRemaining += resourceGenerators[i].defaultTimer;
                        var containsResource = false;
                        for (int j = 0; j < resources.Length; j++)
                        {
                            if (resources[j].type != resource.type) continue;
                            var newResourceHolder = resources[j];
                            newResourceHolder.value += resource.value;
                            resources[j] = newResourceHolder;
                            containsResource = true;
                        }

                        if (!containsResource)
                        {
                            var newResource = new ResourceHolder
                            {
                                type = resource.type,
                                value = resource.value
                            };
                            resources.Add(newResource);
                        }
                    }

                    resourceGenerators[i] = resource;
                }
            }
        }
    }
}