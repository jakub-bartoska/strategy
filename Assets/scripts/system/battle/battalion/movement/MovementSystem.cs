using System;
using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using system.battle.enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(AnalyseBattlefieldSystem))]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var fightPairs = SystemAPI.GetSingletonBuffer<FightPair>();
            var movementBlockingPairs = SystemAPI.GetSingletonBuffer<MovementBlockingPair>();
            var unableToMoveBattalions = new NativeParallelMultiHashMap<long, Direction>(300, Allocator.TempJob);

            var movementBlockersMap = new NativeParallelMultiHashMap<long, (long, Direction)>(1000, Allocator.TempJob);
            foreach (var movementBlockingPair in movementBlockingPairs)
            {
                movementBlockersMap.Add(movementBlockingPair.blocker, (movementBlockingPair.victim, movementBlockingPair.direction));
            }

            var possibleReinforcements = SystemAPI.GetSingletonBuffer<PossibleReinforcements>();
            possibleReinforcements.Clear();

            var waitingBattalions = new NativeParallelHashSet<long>(500, Allocator.TempJob);
            new CollectBattalionWaitingPositionsJob
                {
                    waitingBattalions = waitingBattalions.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            foreach (var waitingBattalion in waitingBattalions)
            {
                fillBlockedMovement(waitingBattalion, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
                fillBlockedMovement(waitingBattalion, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
            }

            foreach (var fightPair in fightPairs)
            {
                fillBlockedMovement(fightPair.battalionId1, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
                fillBlockedMovement(fightPair.battalionId1, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
                fillBlockedMovement(fightPair.battalionId2, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
                fillBlockedMovement(fightPair.battalionId2, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            new MoveBattalionJob
                {
                    deltaTime = deltaTime,
                    unableToMoveBattalions = unableToMoveBattalions
                }.Schedule(state.Dependency)
                .Complete();
        }

        private void fillBlockedMovement(
            long blockedBattalionId,
            Direction direction,
            NativeParallelMultiHashMap<long, (long, Direction)> movementBlockersMap,
            NativeParallelMultiHashMap<long, Direction> unableToMoveBattalions,
            DynamicBuffer<PossibleReinforcements> possibleReinforcements)
        {
            unableToMoveBattalions.Add(blockedBattalionId, direction);
            foreach (var blockedBattalion in movementBlockersMap.GetValuesForKey(blockedBattalionId))
            {
                if (blockedBattalion.Item2 != direction) continue;
                possibleReinforcements.Add(new PossibleReinforcements
                {
                    needHelpBattalionId = blockedBattalionId,
                    canHelpBattalionId = blockedBattalion.Item1
                });
                fillBlockedMovement(blockedBattalion.Item1, blockedBattalion.Item2, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
            }
        }

        public class SortByPosition : IComparer<(long, float3, Team)>
        {
            public int Compare((long, float3, Team) e1, (long, float3, Team) e2)
            {
                return e1.Item2.x.CompareTo(e2.Item2.x);
            }
        }

        [BurstCompile]
        public partial struct CollectBattalionWaitingPositionsJob : IJobEntity
        {
            public NativeParallelHashSet<long>.ParallelWriter waitingBattalions;

            private void Execute(BattalionMarker battalionMarker, WaitForSoldiers wait)
            {
                waitingBattalions.Add(battalionMarker.id);
            }
        }

        [BurstCompile]
        [WithNone(typeof(WaitForSoldiers))]
        public partial struct MoveBattalionJob : IJobEntity
        {
            public float deltaTime;
            public NativeParallelMultiHashMap<long, Direction> unableToMoveBattalions;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform, MovementDirection movementDirection)
            {
                foreach (var direction in unableToMoveBattalions.GetValuesForKey(battalionMarker.id))
                {
                    if (direction == movementDirection.direction) return;
                }

                var speed = 10f * deltaTime;
                //var speed = 1f * deltaTime;
                var directionCoefficient = movementDirection.direction switch
                {
                    Direction.LEFT => -1,
                    Direction.RIGHT => 1,
                    Direction.NONE => 0,
                    Direction.UP => 0,
                    Direction.DOWN => 0,
                    _ => throw new Exception("Unknown direction")
                };

                var delta = new float3(directionCoefficient * speed, 0, 0);
                transform.Position += delta;
            }
        }
    }
}