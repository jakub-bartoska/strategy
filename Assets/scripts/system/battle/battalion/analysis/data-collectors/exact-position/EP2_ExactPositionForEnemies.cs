using component._common.system_switchers;
using component.battle.config;
using system.battle.battalion.analysis.data_holder.movement;
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
            var deltaTime = SystemAPI.Time.DeltaTime;
            var debugConfig = SystemAPI.GetSingleton<DebugConfig>();
            var finalSpeed = debugConfig.speed * deltaTime;

            var inFightMovement = MovementDataHolder.inFightMovement;

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

                MovementDataHolder.battalionExactDistance.Add(kvPair.Key, kvPair.Value.Item2);
            }

            foreach (var kvPair in battalionsToUpdate)
            {
                updateBattalionInFightMovementByHalf(kvPair.Key);
                updateBattalionInFightMovementByHalf(kvPair.Value);
            }
        }

        private void updateBattalionInFightMovementByHalf(long battalionId)
        {
            var currentValue = MovementDataHolder.inFightMovement[battalionId];
            var newDistance = currentValue.Item2 / 2;
            MovementDataHolder.battalionExactDistance.Add(battalionId, newDistance);
        }
    }
}