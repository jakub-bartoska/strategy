using System.Collections.Generic;
using _Monobehaviors.camera;
using component;
using component._common.movement_agents;
using component._common.system_switchers;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.selection;
using component.strategy.town_components;
using ProjectDawn.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.InputSystem;
using utils;

namespace system.strategy.movement
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class StrategyRightClickMovementSystem : SystemBase
    {
        private BattleInputs inputs;

        protected override void OnCreate()
        {
            RequireForUpdate<SelectionMarkerState>();
            RequireForUpdate<PhysicsWorldSingleton>();
            RequireForUpdate<StrategyMapStateMarker>();
            RequireForUpdate<AgentMovementAllowedTag>();
            inputs = InputUtils.getInputs();
            inputs.strategy.MouseRightClick.started += rightStarted;
        }

        protected override void OnUpdate()
        {
        }

        private void rightStarted(InputAction.CallbackContext ctx)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            var position = RaycastUtils.getCurrentMousePosition(SystemAPI.GetSingletonRW<PhysicsWorldSingleton>(), GameCameraType.STRATEGY);
            var movementStatus = findMovementTarget(position);

            var markedArmiesList = new NativeList<long>(100, Allocator.TempJob);
            new ArmyMovementJob
                {
                    movementStatus = movementStatus,
                    markedArmiesList = markedArmiesList.AsParallelWriter(),
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(Dependency)
                .Complete();

            new UpdateInteractionsJob
                {
                    markedArmiesList = markedArmiesList,
                    additionalArmyId = movementStatus.targetArmyId,
                    targetArmyTeam = movementStatus.targetArmyTeam,
                }.ScheduleParallel(Dependency)
                .Complete();
        }

        private ArmyMovementStatus findMovementTarget(float3 click)
        {
            var townsCloseToClick = new NativeList<(long, float)>(15, Allocator.TempJob);
            new FindTownsCloseToClick
                {
                    position = click,
                    townsCloseToClick = townsCloseToClick.AsParallelWriter(),
                    maxDistance = 1
                }.ScheduleParallel(Dependency)
                .Complete();

            if (townsCloseToClick.Length != 0)
            {
                townsCloseToClick.Sort(new SortTownsByDistance());
                var closestTown = townsCloseToClick[0];
                return new ArmyMovementStatus
                {
                    movementType = MovementType.ENTER_TOWN,
                    targetTownId = closestTown.Item1,
                    targetPosition = click
                };
            }

            var armiesCloseToClick = new NativeList<(long, Team, float)>(15, Allocator.TempJob);
            new FindArmiesCloseToClick
                {
                    position = click,
                    armiesCloseToClick = armiesCloseToClick.AsParallelWriter(),
                    //todo pridat do configutraku
                    maxDistance = 1
                }.ScheduleParallel(Dependency)
                .Complete();

            if (armiesCloseToClick.Length == 0)
            {
                return new ArmyMovementStatus
                {
                    movementType = MovementType.MOVE,
                    targetPosition = click
                };
            }

            armiesCloseToClick.Sort(new SortArmiesByDistance());
            var closestArmy = armiesCloseToClick[0];
            return new ArmyMovementStatus
            {
                movementType = MovementType.FOLLOW_ARMY,
                targetArmyId = closestArmy.Item1,
                targetArmyTeam = closestArmy.Item2
            };
        }

        public class SortArmiesByDistance : IComparer<(long, Team, float)>
        {
            public int Compare((long, Team, float) a1, (long, Team, float) a2)
            {
                return a1.Item3.CompareTo(a2.Item3);
            }
        }

        public class SortTownsByDistance : IComparer<(long, float)>
        {
            public int Compare((long, float) a1, (long, float) a2)
            {
                return a1.Item2.CompareTo(a2.Item2);
            }
        }
    }

    public partial struct FindArmiesCloseToClick : IJobEntity
    {
        public float3 position;
        public NativeList<(long, Team, float)>.ParallelWriter armiesCloseToClick;
        public float maxDistance;

        private void Execute(ArmyTag tag, LocalTransform transform, TeamComponent team, IdHolder idHolder)
        {
            var distance = math.distance(position, transform.Position);
            if (distance < maxDistance)
            {
                armiesCloseToClick.AddNoResize((idHolder.id, team.team, distance));
            }
        }
    }

    public partial struct FindTownsCloseToClick : IJobEntity
    {
        public float3 position;
        public NativeList<(long, float)>.ParallelWriter townsCloseToClick;
        public float maxDistance;

        private void Execute(TownTag tag, LocalTransform transform, IdHolder idHolder)
        {
            var distance = math.distance(position, transform.Position);
            if (distance < maxDistance)
            {
                townsCloseToClick.AddNoResize((idHolder.id, distance));
            }
        }
    }

    public partial struct ArmyMovementJob : IJobEntity
    {
        public ArmyMovementStatus movementStatus;
        public NativeList<long>.ParallelWriter markedArmiesList;
        public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute(ArmyMovement armyMovement, ref AgentBody agentBody, ArmyTag tag, Entity entity,
            Marked marked, IdHolder idHolder)
        {
            markedArmiesList.AddNoResize(idHolder.id);

            switch (movementStatus.movementType)
            {
                case MovementType.MOVE:
                case MovementType.ENTER_TOWN:
                    agentBody.IsStopped = false;
                    agentBody.Destination = movementStatus.targetPosition.Value;
                    break;
                case MovementType.FOLLOW_ARMY:
                    if (movementStatus.targetArmyId.Value == idHolder.id)
                    {
                        return;
                    }

                    break;
            }

            ecb.SetComponent((int) idHolder.id, entity, movementStatus);
        }
    }

    public partial struct UpdateInteractionsJob : IJobEntity
    {
        [ReadOnly] public NativeList<long> markedArmiesList;
        [ReadOnly] public long? additionalArmyId;
        [ReadOnly] public Team? targetArmyTeam;

        private void Execute(ArmyMovement armyMovement, ArmyTag tag,
            ref DynamicBuffer<ArmyInteraction> interactionBuffer, TeamComponent team, Marked marked, IdHolder idHolder)
        {
            interactionBuffer.Clear();
            foreach (var armyId in markedArmiesList)
            {
                if (armyId == idHolder.id) continue;

                var interaction = new ArmyInteraction
                {
                    armyId = armyId,
                    interactionType = InteractionType.MERGE_TOGETHER
                };

                interactionBuffer.Add(interaction);
            }

            if (additionalArmyId.HasValue && targetArmyTeam == team.team && additionalArmyId.Value != idHolder.id)
            {
                var interaction = new ArmyInteraction
                {
                    armyId = additionalArmyId.Value,
                    interactionType = InteractionType.MERGE_ME_INTO
                };

                interactionBuffer.Add(interaction);
            }
        }
    }
}