using component._common.system_switchers;
using component.strategy.caravan;
using component.strategy.general;
using component.strategy.player_resources;
using component.strategy.town_components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.strategy.minors.interactions
{
    public partial struct CaravanEnterTownSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var townPositions = new NativeHashMap<long, LocalTransform>(200, Allocator.TempJob);
            new CollectTownsJob
                {
                    townPositions = townPositions
                }.Schedule(state.Dependency)
                .Complete();

            var townIdResourceHolderToAdd = new NativeParallelMultiHashMap<long, ResourceHolder>(100, Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new DestroyFinishedCaravansJob
                {
                    townPositions = townPositions,
                    ecb = ecb.AsParallelWriter(),
                    townIdResourceHolderToAdd = townIdResourceHolderToAdd.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            if (townIdResourceHolderToAdd.IsEmpty) return;

            new UpdateTownResourcesJob
                {
                    townResources = townIdResourceHolderToAdd
                }.ScheduleParallel(state.Dependency)
                .Complete();
        }

        public partial struct CollectTownsJob : IJobEntity
        {
            public NativeHashMap<long, LocalTransform> townPositions;

            private void Execute(TownTag _, LocalTransform transform, IdHolder idHolder)
            {
                townPositions.Add(idHolder.id, transform);
            }
        }

        public partial struct DestroyFinishedCaravansJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<long, LocalTransform> townPositions;
            public EntityCommandBuffer.ParallelWriter ecb;
            public NativeParallelMultiHashMap<long, ResourceHolder>.ParallelWriter townIdResourceHolderToAdd;

            private void Execute(CaravanTarget target, LocalTransform transform, Entity entity, DynamicBuffer<ResourceHolder> resourceHolder)
            {
                townPositions.TryGetValue(target.targetId, out var townTransform);
                if (math.distance(transform.Position, townTransform.Position) > 0.3f) return;

                ecb.DestroyEntity((int) transform.Position.x, entity);
                foreach (var holder in resourceHolder)
                {
                    townIdResourceHolderToAdd.Add(target.targetId, holder);
                }
            }
        }

        public partial struct UpdateTownResourcesJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<long, ResourceHolder> townResources;

            private void Execute(TownTag _, ref DynamicBuffer<ResourceHolder> resources, IdHolder idHolder)
            {
                if (!townResources.ContainsKey(idHolder.id)) return;

                var oldResources = resources.ToNativeArray(Allocator.Temp);
                resources.Clear();
                foreach (var resource in townResources.GetValuesForKey(idHolder.id))
                {
                    var containsResource = false;
                    foreach (var oldResource in oldResources)
                    {
                        if (oldResource.type != resource.type) continue;

                        resources.Add(new ResourceHolder
                        {
                            type = resource.type,
                            value = oldResource.value + resource.value
                        });
                        containsResource = true;
                    }

                    if (!containsResource)
                        resources.Add(resource);
                }

                //add old resources which were not updated
                foreach (var resourceHolder in oldResources)
                {
                    var containsResource = false;
                    foreach (var resource in resources)
                    {
                        if (resource.type != resourceHolder.type) continue;
                        containsResource = true;
                    }

                    if (containsResource) continue;

                    resources.Add(resourceHolder);
                }
            }
        }
    }
}