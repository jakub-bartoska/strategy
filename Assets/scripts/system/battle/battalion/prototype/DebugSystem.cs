using component;
using component._common.system_switchers;
using component.authoring_pairs;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.battle.battalion.cleanup
{
    [UpdateInGroup(typeof(BattleCleanupSystemGroup))]
    public partial struct DebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var dataHolder = SystemAPI.GetSingletonRW<DataHolder>();
            var debugConfigComponent = SystemAPI.GetSingletonRW<DebugConfigComponent>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var newTime = debugConfigComponent.ValueRW.arrowSpawner - deltaTime;
            if (newTime < 0)
            {
                newTime = 2;
                var entity = ecb.Instantiate(prefabHolder.arrowPrefab);
                var transform = LocalTransform.FromPosition(new float3(10050, 0, 10000));
                transform.Scale = 4f;

                BattalionInfo? enemy = null;
                BattalionInfo? me = null;
                foreach (var kvPair in dataHolder.ValueRO.battalionInfo)
                {
                    switch (kvPair.Value.team)
                    {
                        case Team.TEAM1:
                            me = kvPair.Value;
                            break;
                        case Team.TEAM2:
                            enemy = kvPair.Value;
                            break;
                    }
                }

                var distance = math.distance(me.Value.position, enemy.Value.position);
                var normalizedDirectionVector = getNormalizedDirectionVector(me, enemy);
                var angleInRadians = getAngleInRadians(normalizedDirectionVector);
                var arrowMarkerDebug = new ArrowMarkerDebug
                {
                    startingPosition = me.Value.position,
                    flightTime = 0f,
                    distanceCoefficient = distance / 35,
                    rotation = angleInRadians,
                    normalizedDirection = normalizedDirectionVector
                };

                ecb.AddComponent(entity, arrowMarkerDebug);
                ecb.SetComponent(entity, transform);
            }

            debugConfigComponent.ValueRW.arrowSpawner = newTime;

            new DebugJob
                {
                    deltaTime = deltaTime
                }
                .Schedule(state.Dependency)
                .Complete();
        }

        public float3 getNormalizedDirectionVector(BattalionInfo? me, BattalionInfo? enemy)
        {
            var positionDiff = enemy.Value.position - me.Value.position;
            positionDiff.y = 0;
            return math.normalize(positionDiff);
        }

        private float getAngleInRadians(float3 normalizedDirectionVector)
        {
            var angle = Vector3.Angle(new Vector3(1, 0, 0), normalizedDirectionVector);
            Debug.Log("angle: " + angle);
            var resultInRadians = angle * Mathf.PI / 180;
            return resultInRadians;
        }
    }

    [BurstCompile]
    public partial struct DebugJob : IJobEntity
    {
        public float deltaTime;

        private void Execute(ref ArrowMarkerDebug arrowMarkerDebug, ref LocalTransform localTransform)
        {
            if (localTransform.Position.y < (arrowMarkerDebug.startingPosition.y - 0.5f))
            {
                return;
            }

            arrowMarkerDebug.flightTime += deltaTime * 10;

            var x = arrowMarkerDebug.flightTime;
            var y = -0.02f * x * x + 0.7f * x;
            var yDerivation = -0.04f * x + 0.7f;

            var vectorToAdd = x * arrowMarkerDebug.normalizedDirection * arrowMarkerDebug.distanceCoefficient;

            localTransform.Position = new float3(
                arrowMarkerDebug.startingPosition.x + vectorToAdd.x,
                arrowMarkerDebug.startingPosition.y + y * arrowMarkerDebug.distanceCoefficient,
                arrowMarkerDebug.startingPosition.z + vectorToAdd.z
            );

            Debug.Log("rotation: " + arrowMarkerDebug.rotation);

            Debug.Log("current quaternion: " + localTransform.Rotation);

            localTransform.Rotation = quaternion.EulerXYZ(
                0,
                0,
                0
            );
            var newOne = localTransform;
            newOne = newOne.RotateY(-arrowMarkerDebug.rotation);
            newOne = newOne.RotateZ(yDerivation * math.PI / 2);
            localTransform = newOne;
            /*
            localTransform.Rotation = quaternion.EulerXYZ(
                0,
                arrowMarkerDebug.rotation,
                yDerivation * math.PI / 2
            );
            */
        }
    }
}