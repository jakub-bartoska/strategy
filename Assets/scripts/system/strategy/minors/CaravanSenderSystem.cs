using System;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.strategy.caravan;
using component.strategy.general;
using component.strategy.player_resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace system.strategy.minors
{
    public partial struct CaravanSenderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var caravanThreshold = 100;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            new CollectMarkedTownResources
                {
                    caravanThreshold = caravanThreshold,
                    ecb = ecb.AsParallelWriter(),
                    prefabholder = prefabHolder
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        public partial struct CollectMarkedTownResources : IJobEntity
        {
            [ReadOnly] public long caravanThreshold;
            public PrefabHolder prefabholder;
            public EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(IdHolder idHolder, ref DynamicBuffer<ResourceHolder> resources, LocalTransform transform, TeamComponent team)
            {
                switch (idHolder.type)
                {
                    case HolderType.ARMY:
                    case HolderType.TOWN:
                    case HolderType.TOWN_DEPLOYER:
                        return;
                    case HolderType.GOLD_MINE:
                    case HolderType.STONE_MINE:
                    case HolderType.LUMBERJACK_HUT:
                    case HolderType.MILL:
                        break;
                    default: throw new Exception("Unknown holder type");
                }

                var resourcesForCaravan = new NativeList<ResourceHolder>(10, Allocator.Temp);
                var oldResources = resources.ToNativeArray(Allocator.Temp);
                resources.Clear();
                foreach (var resource in oldResources)
                {
                    if (caravanThreshold > resource.value)
                    {
                        resources.Add(resource);
                        continue;
                    }

                    var newValue = resource.value - caravanThreshold;
                    var newResource = new ResourceHolder
                    {
                        type = resource.type,
                        value = newValue
                    };
                    resources.Add(newResource);
                    resourcesForCaravan.Add(new ResourceHolder
                    {
                        type = resource.type,
                        value = caravanThreshold
                    });
                }

                if (resourcesForCaravan.Length == 0) return;

                var caravanEntity = ecb.Instantiate((int) idHolder.id, prefabholder.caravanPrefab);
                ecb.AddComponent((int) idHolder.id + 10000, caravanEntity, new InitCaravanSetting());
                var caravanResources = ecb.AddBuffer<ResourceHolder>((int) idHolder.id + 100000, caravanEntity);
                caravanResources.AddRange(resourcesForCaravan);
                ecb.AddComponent((int) idHolder.id + 1000000, caravanEntity, transform);
                ecb.AddComponent((int) idHolder.id + 10000000, caravanEntity, team);
            }
        }
    }
}