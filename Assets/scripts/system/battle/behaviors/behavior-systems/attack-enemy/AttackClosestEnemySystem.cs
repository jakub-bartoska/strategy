using component._common.system_switchers;
using component.helpers;
using system_groups;
using Unity.Burst;
using Unity.Entities;

namespace system.behaviors.behavior_systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct AttackClosestEnemySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Damage>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damage = SystemAPI.GetSingletonBuffer<Damage>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            new AttackClosestEnemyJob
                {
                    damage = damage,
                    deltaTime = deltaTime
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct AttackClosestEnemyJob : IJobEntity
    {
        public float deltaTime;
        public DynamicBuffer<Damage> damage;

        [BurstCompile]
        private void Execute(AttackClosestEnemyAspect attackClosestEnemyAspect)
        {
            attackClosestEnemyAspect.attackClosestEnemy(deltaTime, damage);
        }
    }
}