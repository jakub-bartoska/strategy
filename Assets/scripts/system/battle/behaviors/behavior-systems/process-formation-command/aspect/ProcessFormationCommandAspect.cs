using System;
using component;
using component.formation;
using component.soldier;
using component.soldier.behavior.behaviors;
using ProjectDawn.Navigation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace system.behaviors.behavior_systems.process_formation_command.aspect
{
    public readonly partial struct ProcessFormationCommandAspect : IAspect
    {
        private readonly RefRO<SoldierFormationStatus> soldierFormationStatus;
        private readonly RefRO<SoldierStatus> soldierStatus;
        private readonly RefRW<AgentBody> agentBody;
        private readonly RefRO<BehaviorContext> context;

        public void execute(UnsafeList<FormationContext> formations)
        {
            if (context.ValueRO.currentBehavior != BehaviorType.PROCESS_FORMATION_COMMAND)
            {
                return;
            }

            var formation = pickMyFormation(formations);
            var myDestination = getMyFormationpositio(formation);
            agentBody.ValueRW.IsStopped = false;
            agentBody.ValueRW.Destination = myDestination;
        }

        private FormationContext pickMyFormation(UnsafeList<FormationContext> formations)
        {
            foreach (var formation in formations)
            {
                if (formation.id == soldierFormationStatus.ValueRO.formationId)
                {
                    return formation;
                }
            }

            throw new Exception("Formation not found");
        }

        private float3 getMyFormationpositio(FormationContext formationContext)
        {
            var center = formationContext.formationCenter;
            var formationIndex = formationContext.soldierIdToFormationIndex[soldierStatus.ValueRO.index];
            var myZ = formationContext.formationSize * 0.5f * formationContext.distanceBetweenSoldiers +
                      center.z -
                      (formationIndex * formationContext.distanceBetweenSoldiers);
            return new float3(center.x, 0, myZ);
        }
    }
}