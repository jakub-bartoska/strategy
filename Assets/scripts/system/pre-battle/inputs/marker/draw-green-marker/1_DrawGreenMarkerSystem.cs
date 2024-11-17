using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.pre_battle.inputs.draw_green_marker
{
    public partial struct DrawGreenMarkerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var preBattlePositionMarker = SystemAPI.GetSingleton<PreBattlePositionMarker>();
            if (preBattlePositionMarker.state != PreBattleMarkerState.RUNNING)
            {
                destroyMarker(state.EntityManager);
                return;
            }

            if (SystemAPI.TryGetSingletonEntity<PreBattleGreenMarker>(out var entity))
            {
                updatePositionAndScale(entity, state.EntityManager, preBattlePositionMarker);
                return;
            }

            createMarker(state.EntityManager, preBattlePositionMarker);
        }

        private void destroyMarker(EntityManager entityManager)
        {
            if (SystemAPI.TryGetSingletonEntity<PreBattleGreenMarker>(out var entity))
            {
                entityManager.DestroyEntity(entity);
            }
        }

        private void updatePositionAndScale(Entity entity, EntityManager entityManager, PreBattlePositionMarker preBattlePositionMarker)
        {
            var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            var position = getPositionFromMarker(preBattlePositionMarker);
            localTransform.Position = position;

            var newScale = getScaleFromMarker(preBattlePositionMarker);
            var scale = entityManager.GetComponentData<PostTransformMatrix>(entity);
            scale.Value = newScale;

            entityManager.SetComponentData(entity, localTransform);
            entityManager.SetComponentData(entity, scale);
        }

        private void createMarker(EntityManager entityManager, PreBattlePositionMarker preBattlePositionMarker)
        {
            var preBattleMarkerPrefab = SystemAPI.GetSingleton<PrefabHolder>().preBattleMarkerPrefab;

            var newEntity = entityManager.Instantiate(preBattleMarkerPrefab);
            var marker = new PreBattleGreenMarker();
            var position = getPositionFromMarker(preBattlePositionMarker);
            var transform = LocalTransform.FromPosition(position);
            transform.Rotation = quaternion.Euler(0.5f * math.PI, 0, 0);
            var scale = getScaleFromMarker(preBattlePositionMarker);
            var postTransformScale = new PostTransformMatrix
            {
                Value = scale
            };

            entityManager.AddComponentData(newEntity, marker);
            entityManager.AddComponentData(newEntity, postTransformScale);
            entityManager.SetComponentData(newEntity, transform);
        }

        private float4x4 getScaleFromMarker(PreBattlePositionMarker preBattlePositionMarker)
        {
            var position = createPositions(preBattlePositionMarker);
            var x = position.maxX - position.minX;
            var z = position.maxZ - position.minZ;
            //z is set into y because I rotate green marker by 90 degrees
            return float4x4.Scale(x, z, 1);
        }

        private float3 getPositionFromMarker(PreBattlePositionMarker preBattlePositionMarker)
        {
            var position = createPositions(preBattlePositionMarker);
            var x = (position.minX + position.maxX) / 2;
            var z = (position.minZ + position.maxZ) / 2;
            return new float3(x, 1, z);
        }

        private RunningSystem.MarkerPositions createPositions(PreBattlePositionMarker preBattlePositionMarker)
        {
            return new RunningSystem.MarkerPositions
            {
                minX = math.min(preBattlePositionMarker.startPosition.Value.x, preBattlePositionMarker.endPosition.Value.x),
                maxX = math.max(preBattlePositionMarker.startPosition.Value.x, preBattlePositionMarker.endPosition.Value.x),
                minZ = math.min(preBattlePositionMarker.startPosition.Value.y, preBattlePositionMarker.endPosition.Value.y),
                maxZ = math.max(preBattlePositionMarker.startPosition.Value.y, preBattlePositionMarker.endPosition.Value.y)
            };
        }
    }
}