using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.shadow;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    [UpdateAfter(typeof(DestroyKilledSoldiersSystem))]
    public partial struct RemoveEmptyBattalionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var shadowsToDestroy = new NativeHashSet<long>(1000, Allocator.TempJob);
            new DestroyEmptyBattalionsJob
                {
                    ecb = ecb,
                    shadowsToDestroy = shadowsToDestroy
                }.Schedule(state.Dependency)
                .Complete();

            if (shadowsToDestroy.IsEmpty)
            {
                shadowsToDestroy.Dispose();
                return;
            }

            new DestroyEmptyShadows
                {
                    ecb = ecb,
                    shadowsToDestroy = shadowsToDestroy
                }.Schedule(state.Dependency)
                .Complete();

            shadowsToDestroy.Dispose();
        }

        [BurstCompile]
        public partial struct DestroyEmptyBattalionsJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public NativeHashSet<long> shadowsToDestroy;

            private void Execute(DynamicBuffer<BattalionSoldiers> soldiers, Entity entity, BattalionMarker battalionMarker)
            {
                if (soldiers.Length == 0)
                {
                    shadowsToDestroy.Add(battalionMarker.id);
                    ecb.DestroyEntity(entity);
                }
            }
        }

        [BurstCompile]
        public partial struct DestroyEmptyShadows : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public NativeHashSet<long> shadowsToDestroy;

            private void Execute(BattalionShadowMarker battalionShadowMarker, Entity entity)
            {
                if (shadowsToDestroy.Contains(battalionShadowMarker.parentBattalionId))
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}