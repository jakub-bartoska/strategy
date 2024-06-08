using System;
using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
using component.battle.config;
using system.battle.enums;
using system.battle.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion
{
    [UpdateAfter(typeof(AnalyseBattlefieldSystem))]
    public partial struct MovementSystemOld : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugConfig>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<BattalionMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            /*
            var speed = SystemAPI.GetSingleton<DebugConfig>().speed;

            var fightPairs = SystemAPI.GetSingletonBuffer<FightPair>();
            var movementBlockingPairs = SystemAPI.GetSingletonBuffer<MovementBlockingPair>();
            //battalion id -> movement direction
            var unableToMoveBattalions = new NativeParallelMultiHashMap<long, Direction>(300, Allocator.TempJob);//ok

            //blocker id -> victim id, direction
            var movementBlockersMap = new NativeParallelMultiHashMap<long, (long, Direction)>(1000, Allocator.TempJob);//ok
            var shadowBlockers = new NativeParallelMultiHashMap<long, (long, Direction)>(1000, Allocator.TempJob);//ok
            //battalion id -> direction I want move, position
            var battalionInfo = new NativeHashMap<long, (MovementDirection, float3, BattalionWidth)>(1000, Allocator.TempJob);//ok
            //battalion id -> target position
            var exactPositionTarget = new NativeHashMap<long, float3>(1000, Allocator.TempJob);//ok

            foreach (var movementBlockingPair in movementBlockingPairs)
            {
                switch (movementBlockingPair.blockerType)
                {
                    case BattleUnitTypeEnum.BATTALION:
                        movementBlockersMap.Add(movementBlockingPair.blocker, (movementBlockingPair.victim, movementBlockingPair.direction));
                        break;
                    case BattleUnitTypeEnum.SHADOW:
                        shadowBlockers.Add(movementBlockingPair.blocker, (movementBlockingPair.victim, movementBlockingPair.direction));
                        break;
                }
            }

            new CollectMovementDirections
                {
                    movementDirections = battalionInfo
                }.Schedule(state.Dependency)
                .Complete();

            var possibleReinforcements = SystemAPI.GetSingletonBuffer<PossibleReinforcements>();
            possibleReinforcements.Clear();

            var waitingBattalions = new NativeParallelHashSet<long>(500, Allocator.TempJob);//ok
            new CollectBattalionWaitingPositionsJob
                {
                    waitingBattalions = waitingBattalions.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            foreach (var waitingBattalion in waitingBattalions)
            {
                fillBlockedMovement(waitingBattalion, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, battalionInfo, exactPositionTarget);
                fillBlockedMovement(waitingBattalion, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, battalionInfo, exactPositionTarget);
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
                    fillBlockedMovement(fightPair.battalionId1, direction, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, battalionInfo, exactPositionTarget);
                    fillBlockedMovement(fightPair.battalionId2, direction, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, battalionInfo, exactPositionTarget);
                }

                fillExactPositionForFightingPair(fightPair, exactPositionTarget, battalionInfo);
            }

            foreach (var shadowBlocker in shadowBlockers)
            {
                fillBlockedMovement(shadowBlocker.Value.Item1, shadowBlocker.Value.Item2, movementBlockersMap, unableToMoveBattalions, possibleReinforcements, battalionInfo, exactPositionTarget);
                var exactPosition = CustomTransformUtils.calculateDesiredPosition(
                    battalionInfo[shadowBlocker.Key].Item2,
                    battalionInfo[shadowBlocker.Value.Item1].Item3,
                    battalionInfo[shadowBlocker.Key].Item3,
                    shadowBlocker.Value.Item2,
                    true);
                exactPositionTarget.TryAdd(shadowBlocker.Value.Item1, exactPosition);
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            new ExactPositionTagger
                {
                    ecb = ecb,
                    exactPositionsForBattalion = exactPositionTarget
                }.Schedule(state.Dependency)
                .Complete();
            new ExactPositionTagger2
                {
                    ecb = ecb,
                    exactPositionsForBattalion = exactPositionTarget
                }.Schedule(state.Dependency)
                .Complete();

            var deltaTime = SystemAPI.Time.DeltaTime;
            new MoveBattalionJob
                {
                    deltaTime = deltaTime,
                    unableToMoveBattalions = unableToMoveBattalions,
                    speed = speed
                }.Schedule(state.Dependency)
                .Complete();

            new MoveBattalionJobForExactPositions
                {
                    deltaTime = deltaTime,
                    speed = speed
                }.Schedule(state.Dependency)
                .Complete();
                */
        }

        private void fillExactPositionForFightingPair(
            FightPair fightPair,
            NativeHashMap<long, float3> exactPositionTarget,
            NativeHashMap<long, (MovementDirection, float3, BattalionWidth)> battalionInfo)
        {
            var lowerId = fightPair.battalionId1 < fightPair.battalionId2 ? fightPair.battalionId1 : fightPair.battalionId2;
            var higherId = fightPair.battalionId1 < fightPair.battalionId2 ? fightPair.battalionId2 : fightPair.battalionId1;
            var direction = lowerId == fightPair.battalionId1 ? fightPair.direction : getOpositeDirection(fightPair.direction);
            var exactPosition = CustomTransformUtils.calculateDesiredPosition(
                battalionInfo[lowerId].Item2,
                battalionInfo[higherId].Item3,
                battalionInfo[lowerId].Item3,
                direction,
                true);
            exactPositionTarget.TryAdd(higherId, exactPosition);
        }

        private Direction getOpositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.LEFT:
                    return Direction.RIGHT;
                case Direction.RIGHT:
                    return Direction.LEFT;
                case Direction.UP:
                    return Direction.DOWN;
                case Direction.DOWN:
                    return Direction.UP;
                default:
                    throw new Exception("Unknown direction");
            }
        }

        private void fillBlockedMovement(
            long blockedBattalionId,
            Direction direction,
            NativeParallelMultiHashMap<long, (long, Direction)> movementBlockersMap,
            NativeParallelMultiHashMap<long, Direction> unableToMoveBattalions,
            NativeHashMap<long, (MovementDirection, float3, BattalionWidth)> battalionInfo,
            NativeHashMap<long, float3> exactPositionTarget)
        {
            unableToMoveBattalions.Add(blockedBattalionId, direction);

            foreach (var blockedBattalion in movementBlockersMap.GetValuesForKey(blockedBattalionId))
            {
                var blockedBattalionDirection = battalionInfo[blockedBattalion.Item1].Item1.defaultDirection;
                if (blockedBattalionDirection != direction) continue;

                if (blockedBattalion.Item2 != direction) continue;

                var exactPosition = CustomTransformUtils.calculateDesiredPosition(
                    battalionInfo[blockedBattalionId].Item2,
                    battalionInfo[blockedBattalion.Item1].Item3,
                    battalionInfo[blockedBattalionId].Item3,
                    direction,
                    true);
                exactPositionTarget.TryAdd(blockedBattalion.Item1, exactPosition);
                if (direction == Direction.UP || direction == Direction.DOWN)
                {
                    fillBlockedMovement(blockedBattalion.Item1, Direction.LEFT, movementBlockersMap, unableToMoveBattalions, battalionInfo, exactPositionTarget);
                    fillBlockedMovement(blockedBattalion.Item1, Direction.RIGHT, movementBlockersMap, unableToMoveBattalions, battalionInfo, exactPositionTarget);
                }

                fillBlockedMovement(blockedBattalion.Item1, blockedBattalion.Item2, movementBlockersMap, unableToMoveBattalions, battalionInfo, exactPositionTarget);
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
        public partial struct ExactPositionTagger : IJobEntity
        {
            public NativeHashMap<long, float3> exactPositionsForBattalion;
            public EntityCommandBuffer ecb;

            private void Execute(BattalionMarker battalionMarker, ref MoveToExactPosition moveToExactPosition, Entity entity)
            {
                if (exactPositionsForBattalion.TryGetValue(battalionMarker.id, out var exactPosition))
                {
                    moveToExactPosition.targetPosition = exactPosition;
                }
                else
                {
                    ecb.RemoveComponent<MoveToExactPosition>(entity);
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(MoveToExactPosition))]
        public partial struct ExactPositionTagger2 : IJobEntity
        {
            public NativeHashMap<long, float3> exactPositionsForBattalion;
            public EntityCommandBuffer ecb;

            private void Execute(BattalionMarker battalionMarker, Entity entity)
            {
                if (exactPositionsForBattalion.TryGetValue(battalionMarker.id, out var exactPosition))
                {
                    ecb.AddComponent(entity, new MoveToExactPosition
                    {
                        targetPosition = exactPosition
                    });
                }
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
            public NativeHashMap<long, (MovementDirection, float3, BattalionWidth)> movementDirections;

            private void Execute(BattalionMarker battalionMarker, MovementDirection movementDirection, LocalTransform transform, BattalionWidth battalionWidth)
            {
                movementDirections.Add(battalionMarker.id, (movementDirection, transform.Position, battalionWidth));
            }
        }

        [BurstCompile]
        [WithNone(typeof(WaitForSoldiers))]
        [WithNone(typeof(MoveToExactPosition))]
        public partial struct MoveBattalionJob : IJobEntity
        {
            public float deltaTime;
            public NativeParallelMultiHashMap<long, Direction> unableToMoveBattalions;
            public float speed;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform, MovementDirection movementDirection)
            {
                foreach (var direction in unableToMoveBattalions.GetValuesForKey(battalionMarker.id))
                {
                    if (direction == movementDirection.defaultDirection)
                    {
                        return;
                    }
                }

                var finalSpeed = speed * deltaTime;
                var directionCoefficient = movementDirection.defaultDirection switch
                {
                    Direction.LEFT => -1,
                    Direction.RIGHT => 1,
                    Direction.NONE => 0,
                    Direction.UP => 0,
                    Direction.DOWN => 0,
                    _ => throw new Exception("Unknown direction")
                };

                var delta = new float3(directionCoefficient * finalSpeed, 0, 0);
                transform.Position += delta;
            }
        }

        [BurstCompile]
        [WithNone(typeof(WaitForSoldiers))]
        public partial struct MoveBattalionJobForExactPositions : IJobEntity
        {
            public float deltaTime;
            public float speed;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform, ref MoveToExactPosition moveToExactPosition)
            {
                return;

                var finalSpeed = speed * deltaTime;
                var distance = math.distance(transform.Position, moveToExactPosition.targetPosition);
                if (distance < finalSpeed)
                {
                    transform.Position = moveToExactPosition.targetPosition;
                    return;
                }

                var directionVector = moveToExactPosition.targetPosition - transform.Position;
                var normalizedDirectionVector = math.normalize(directionVector);
                var delta = normalizedDirectionVector * finalSpeed;
                transform.Position += delta;
            }
        }
    }
}