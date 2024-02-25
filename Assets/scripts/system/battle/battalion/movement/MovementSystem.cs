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
            var shadowBlockers = new NativeParallelMultiHashMap<long, (long, Direction)>(1000, Allocator.TempJob);
            var movementDirections = new NativeHashMap<long, MovementDirection>(1000, Allocator.TempJob);
            foreach (var movementBlockingPair in movementBlockingPairs)
            {
                switch (movementBlockingPair.blockerType)
                {
                    case BlockerType.BATTALION:
                        movementBlockersMap.Add(movementBlockingPair.blocker, (movementBlockingPair.victim, movementBlockingPair.direction));
                        break;
                    case BlockerType.SHADOW:
                        shadowBlockers.Add(movementBlockingPair.blocker, (movementBlockingPair.victim, movementBlockingPair.direction));
                        break;
                }
            }

            new CollectMovementDirections
                {
                    movementDirections = movementDirections
                }.Schedule(state.Dependency)
                .Complete();

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
                fillBlockedMovement(waitingBattalion, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
                fillBlockedMovement(waitingBattalion, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
            }

            var allDirections = new NativeList<Direction>(4, Allocator.Temp);
            allDirections.Add(Direction.LEFT);
            allDirections.Add(Direction.RIGHT);
            allDirections.Add(Direction.UP);
            allDirections.Add(Direction.DOWN);

            foreach (var fightPair in fightPairs)
            {
                foreach (var direction in allDirections)
                {
                    fillBlockedMovement(fightPair.battalionId1, direction, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
                    fillBlockedMovement(fightPair.battalionId2, direction, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
                }
            }

            foreach (var shadowBlocker in shadowBlockers)
            {
                fillBlockedMovement(shadowBlocker.Value.Item1, shadowBlocker.Value.Item2, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
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
            DynamicBuffer<PossibleReinforcements> possibleReinforcements,
            NativeHashMap<long, MovementDirection> movementDirections)
        {
            unableToMoveBattalions.Add(blockedBattalionId, direction);
            foreach (var blockedBattalion in movementBlockersMap.GetValuesForKey(blockedBattalionId))
            {
                var blockedBattalionDirection = movementDirections[blockedBattalion.Item1].direction;
                if (blockedBattalionDirection != direction) continue;

                if (blockedBattalion.Item2 != direction) continue;

                possibleReinforcements.Add(new PossibleReinforcements
                {
                    needHelpBattalionId = blockedBattalionId,
                    canHelpBattalionId = blockedBattalion.Item1
                });
                if (direction == Direction.UP || direction == Direction.DOWN)
                {
                    fillBlockedMovement(blockedBattalion.Item1, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
                    fillBlockedMovement(blockedBattalion.Item1, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
                }

                fillBlockedMovement(blockedBattalion.Item1, blockedBattalion.Item2, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, movementDirections);
            }
        }

        public class SortByPosition : IComparer<(long, float3, Team, float)>
        {
            public int Compare((long, float3, Team, float) e1, (long, float3, Team, float) e2)
            {
                return e1.Item2.x.CompareTo(e2.Item2.x);
            }
        }

        [BurstCompile]
        [WithAll(typeof(WaitForSoldiers))]
        public partial struct CollectBattalionWaitingPositionsJob : IJobEntity
        {
            public NativeParallelHashSet<long>.ParallelWriter waitingBattalions;

            private void Execute(BattalionMarker battalionMarker)
            {
                waitingBattalions.Add(battalionMarker.id);
            }
        }

        [BurstCompile]
        public partial struct CollectMovementDirections : IJobEntity
        {
            public NativeHashMap<long, MovementDirection> movementDirections;

            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection)
            {
                movementDirections.Add(battalionMarker.id, movementDirection);
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