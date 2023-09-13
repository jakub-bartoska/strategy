using component._common.movement_agents;
using component._common.system_switchers;
using component.formation;
using system_groups;
using system.behaviors.behavior_systems.process_formation_command.aspect;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace system.behaviors.behavior_systems.process_formation_command
{
    [BurstCompile]
    [UpdateInGroup(typeof(BehaviorSystemGroup))]
    public partial struct ProcessFormationCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<FormationContext>();
            state.RequireForUpdate<AgentMovementAllowedForBattleTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var formations = new UnsafeList<FormationContext>(100, Allocator.TempJob);
            foreach (var formationContext in SystemAPI.Query<FormationContext>())
            {
                formations.Add(formationContext);
            }

            //todo schedule paralel
            new ProcessFormationJob
                {
                    formations = formations
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct ProcessFormationJob : IJobEntity
    {
        public UnsafeList<FormationContext> formations;

        private void Execute(ProcessFormationCommandAspect aspect)
        {
            aspect.execute(formations);
        }
    }
}