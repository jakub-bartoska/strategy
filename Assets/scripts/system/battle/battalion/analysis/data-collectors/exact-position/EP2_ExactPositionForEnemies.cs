using component._common.system_switchers;
using component.battle.battalion.data_holders;
using component.battle.config;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.analysis.exact_position
{
    [UpdateInGroup(typeof(BattleAnalysisSystemGroup))]
    [UpdateAfter(typeof(EP1_DiagonalFightExactPosition))]
    public partial struct EP2_ExactPositionForEnemies : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var movementDataHolder = SystemAPI.GetSingletonRW<MovementDataHolder>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var debugConfig = SystemAPI.GetSingleton<DebugConfig>();
            var finalSpeed = debugConfig.speed * deltaTime;

            var inFightMovement = movementDataHolder.ValueRO.inFightMovement;

            var battalionsToUpdate = new NativeHashMap<long, long>(1000, Allocator.Temp);

            foreach (var kvPair in inFightMovement)
            {
                if (kvPair.Value.Item2 > finalSpeed)
                {
                    continue;
                }

                if (inFightMovement.TryGetValue(kvPair.Value.Item3, out var closestenemy))
                {
                    if (closestenemy.Item3 == kvPair.Key)
                    {
                        if (kvPair.Key < kvPair.Value.Item3)
                        {
                            battalionsToUpdate.TryAdd(kvPair.Key, kvPair.Value.Item3);
                        }
                        else
                        {
                            battalionsToUpdate.TryAdd(kvPair.Value.Item3, kvPair.Key);
                        }

                        continue;
                    }
                }

                movementDataHolder.ValueRW.battalionExactDistance.Add(kvPair.Key, kvPair.Value.Item2);
            }

            foreach (var kvPair in battalionsToUpdate)
            {
                updateBattalionInFightMovementByHalf(kvPair.Key, movementDataHolder);
                updateBattalionInFightMovementByHalf(kvPair.Value, movementDataHolder);
            }
        }

        private void updateBattalionInFightMovementByHalf(long battalionId, RefRW<MovementDataHolder> movementDataHolder)
        {
            var currentValue = movementDataHolder.ValueRO.inFightMovement[battalionId];
            var newDistance = currentValue.Item2 / 2;
            movementDataHolder.ValueRW.battalionExactDistance.Add(battalionId, newDistance);
        }
    }
}