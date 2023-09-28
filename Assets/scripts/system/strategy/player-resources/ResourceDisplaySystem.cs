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
    public partial struct ResourceDisplaySystem : ISystem
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
            var playerTeam = Team.TEAM1;
            var allPlayerResources = new NativeList<ResourceHolder>(Allocator.TempJob);

            new GatherPlayerResources
                {
                    playerTeam = playerTeam,
                    allPlayerResources = allPlayerResources
                }.Schedule(state.Dependency)
                .Complete();

            foreach (var holder in allPlayerResources)
            {
                ResourcesUi.instance.updateResource(holder.type, holder.value);
            }
        }

        [BurstCompile]
        public partial struct GatherPlayerResources : IJobEntity
        {
            [ReadOnly] public Team playerTeam;
            public NativeList<ResourceHolder> allPlayerResources;

            private void Execute(TeamComponent team, DynamicBuffer<ResourceHolder> resources)
            {
                if (team.team != playerTeam) return;

                foreach (var resource in resources)
                {
                    for (int i = 0; i < allPlayerResources.Length; i++)
                    {
                        if (resource.type != allPlayerResources[i].type) continue;

                        var newValue = allPlayerResources[i];
                        newValue.value += resource.value;
                        allPlayerResources[i] = newValue;
                        goto outerLoop;
                    }

                    allPlayerResources.Add(resource);
                    outerLoop:
                    continue;
                }
            }
        }
    }
}