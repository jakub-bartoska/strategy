using System;
using component;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.strategy.general;
using component.strategy.player_resources;
using system.strategy.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

            var caravansToSpawn = new NativeParallelMultiHashMap<long, (float3, Team, ResourceHolder)>(1000, Allocator.TempJob);
            new CollectMarkedTownResources
                {
                    caravanThreshold = caravanThreshold,
                    caravansToSpawn = caravansToSpawn
                }.Schedule(state.Dependency)
                .Complete();

            if (caravansToSpawn.IsEmpty) return;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var idGenerator = SystemAPI.GetSingletonRW<IdGenerator>();

            var keys = caravansToSpawn.GetUniqueKeyArray(Allocator.TempJob);
            foreach (var key in keys.Item1.GetSubArray(0, keys.Item2))
            {
                var position = new float3();
                var team = Team.TEAM1;
                var resourceHolders = new NativeList<ResourceHolder>(Allocator.Temp);
                foreach (var (position2, team2, resource2) in caravansToSpawn.GetValuesForKey(key))
                {
                    position = position2;
                    team = team2;
                    resourceHolders.Add(resource2);
                }

                CaravanSpawner.spawnCaravan(prefabHolder, ecb, position, team, resourceHolders, idGenerator);
            }
        }

        public partial struct CollectMarkedTownResources : IJobEntity
        {
            [ReadOnly] public long caravanThreshold;
            public NativeParallelMultiHashMap<long, (float3, Team, ResourceHolder)> caravansToSpawn;

            private void Execute(IdHolder idHolder, ref DynamicBuffer<ResourceHolder> resources, LocalTransform transform, TeamComponent team)
            {
                switch (idHolder.type)
                {
                    case HolderType.ARMY:
                    case HolderType.TOWN:
                    case HolderType.TOWN_DEPLOYER:
                    case HolderType.CARAVAN:
                        return;
                    case HolderType.GOLD_MINE:
                    case HolderType.STONE_MINE:
                    case HolderType.LUMBERJACK_HUT:
                    case HolderType.MILL:
                        break;
                    default: throw new Exception("Unknown holder type");
                }

                for (int i = 0; i < resources.Length; i++)
                {
                    var resource = resources[i];
                    if (caravanThreshold > resource.value) continue;

                    var newValue = resource.value - caravanThreshold;
                    var newResource = new ResourceHolder
                    {
                        type = resource.type,
                        value = newValue
                    };
                    resources[i] = newResource;
                    var resourceForCaravan = resource;
                    resourceForCaravan.value = caravanThreshold;
                    caravansToSpawn.Add(idHolder.id, (transform.Position, team.team, resourceForCaravan));
                }
            }
        }
    }
}