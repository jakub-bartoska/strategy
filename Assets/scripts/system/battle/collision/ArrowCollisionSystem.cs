using component;
using component._common.system_switchers;
using component.authoring_pairs;
using component.config.authoring_pairs;
using component.helpers;
using system.general;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace system.collision
{
    [BurstCompile]
    [UpdateBefore(typeof(DamageSystem))]
    public partial struct ArrowCollisionSystem : ISystem
    {
        private ComponentLookup<ArrowMarker> arrowMarkerLookup;
        private ComponentLookup<SoldierStatus> soldierStatusLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<Damage>();
            state.RequireForUpdate<ArrowConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();

            arrowMarkerLookup = state.GetComponentLookup<ArrowMarker>();
            soldierStatusLookup = state.GetComponentLookup<SoldierStatus>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            arrowMarkerLookup.Update(ref state);
            soldierStatusLookup.Update(ref state);
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            var damage = SystemAPI.GetSingletonBuffer<Damage>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var arrowConfig = SystemAPI.GetSingleton<ArrowConfig>();

            state.Dependency = new CollisionJob
            {
                dmgReceived = damage,
                ecb = ecb.AsParallelWriter(),
                arrowMarkerLookup = arrowMarkerLookup,
                soldierStatusLookup = soldierStatusLookup,
                arrowConfig = arrowConfig,
            }.Schedule(simulation, state.Dependency);
        }
    }

    [BurstCompile]
    public struct CollisionJob : ITriggerEventsJob
    {
        public DynamicBuffer<Damage> dmgReceived;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public ComponentLookup<ArrowMarker> arrowMarkerLookup;
        [ReadOnly] public ComponentLookup<SoldierStatus> soldierStatusLookup;
        [ReadOnly] public ArrowConfig arrowConfig;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityAType = getEntityType(triggerEvent.EntityA);
            var entityBType = getEntityType(triggerEvent.EntityB);

            if (entityAType == ColliderType.UNKNOWN || entityBType == ColliderType.UNKNOWN)
            {
                return;
            }

            var arrowEntity = entityAType == ColliderType.ARROW ? triggerEvent.EntityA : triggerEvent.EntityB;
            var soldierEntity = entityBType == ColliderType.SOLDIER ? triggerEvent.EntityB : triggerEvent.EntityA;

            var arrowMarker = arrowMarkerLookup.GetRefRO(arrowEntity);
            var soldierStatus = soldierStatusLookup.GetRefRO(soldierEntity);

            if (arrowMarker.ValueRO.team == soldierStatus.ValueRO.team)
            {
                return;
            }

            ecb.DestroyEntity(arrowEntity.Index, arrowEntity);

            var soldierIndex = soldierStatus.ValueRO.index;
            dmgReceived.Add(new Damage
            {
                dmgReceiverId = soldierIndex,
                dmgAmount = arrowConfig.arrowDamage
            });
        }

        private ColliderType getEntityType(Entity entity)
        {
            if (arrowMarkerLookup.HasComponent(entity))
            {
                return ColliderType.ARROW;
            }

            if (soldierStatusLookup.HasComponent(entity))
            {
                return ColliderType.SOLDIER;
            }

            return ColliderType.UNKNOWN;
        }

        private enum ColliderType
        {
            ARROW,
            SOLDIER,
            UNKNOWN
        }
    }
}