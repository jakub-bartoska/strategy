using System.Collections.Generic;
using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.markers;
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
            var unableToMoveBattalions = new NativeHashSet<long>(300, Allocator.TempJob);

            var movementBlockersMap = new NativeParallelMultiHashMap<long, long>(1000, Allocator.TempJob);
            foreach (var movementBlockingPair in movementBlockingPairs)
            {
                movementBlockersMap.Add(movementBlockingPair.blocker, movementBlockingPair.victim);
            }

            var possibleReinforcements = SystemAPI.GetSingletonBuffer<PossibleReinforcements>();
            possibleReinforcements.Clear();

            foreach (var fightPair in fightPairs)
            {
                fillBlockedMovement(fightPair.battalionId1, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
                fillBlockedMovement(fightPair.battalionId2, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
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
            NativeParallelMultiHashMap<long, long> movementBlockersMap,
            NativeHashSet<long> unableToMoveBattalions,
            DynamicBuffer<PossibleReinforcements> possibleReinforcements)
        {
            unableToMoveBattalions.Add(blockedBattalionId);
            foreach (var blockedBattalion in movementBlockersMap.GetValuesForKey(blockedBattalionId))
            {
                possibleReinforcements.Add(new PossibleReinforcements
                {
                    needHelpBattalionId = blockedBattalionId,
                    canHelpBattalionId = blockedBattalion
                });
                fillBlockedMovement(blockedBattalion, movementBlockersMap, unableToMoveBattalions, possibleReinforcements);
            }
        }

        public class SortByPosition : IComparer<(long, float3, Team)>
        {
            public int Compare((long, float3, Team) e1, (long, float3, Team) e2)
            {
                if (e1.Item3 == e2.Item3)
                {
                    return e1.Item2.x.CompareTo(e2.Item2.x);
                }

                if (e1.Item3 == Team.TEAM1) return 1;

                return -1;
            }
        }

        [BurstCompile]
        public partial struct CollectBattalionPositionsJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, (long, float3, Team)>.ParallelWriter battalionPositions;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform)
            {
                battalionPositions.Add(battalionMarker.row,
                    (battalionMarker.id, transform.Position, battalionMarker.team));
            }
        }

        [BurstCompile]
        public partial struct MoveBattalionJob : IJobEntity
        {
            public float deltaTime;
            public NativeHashSet<long> unableToMoveBattalions;

            private void Execute(BattalionMarker battalionMarker, ref LocalTransform transform)
            {
                if (unableToMoveBattalions.Contains(battalionMarker.id)) return;

                var speed = 10f * deltaTime;
                var direction = battalionMarker.team == Team.TEAM1 ? -1 : 1;

                var delta = new float3(direction * speed, 0, 0);
                transform.Position += delta;
            }
        }
    }
}