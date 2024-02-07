using component.authoring_pairs.PrefabHolder;
using component.battle.battalion.shadow;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class BattalionShadowSpawner
    {
        public static Entity spawnBattalionShadow(EntityCommandBuffer.ParallelWriter ecb, PrefabHolder prefabHolder, float3 battalionPosition, long parentBattalionId)
        {
            var battalionShadowPrefab = prefabHolder.battalionShadowPrefab;
            var newBattalionShadow = ecb.Instantiate(0, battalionShadowPrefab);
            var battalionShadowMarker = new BattalionShadowMarker
            {
                parentBattalionId = parentBattalionId
            };

            //todo upravit pozici podle row
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);

            ecb.AddComponent(0, newBattalionShadow, battalionShadowMarker);
            ecb.SetComponent(0, newBattalionShadow, battalionTransform);

            return newBattalionShadow;
        }
    }
}