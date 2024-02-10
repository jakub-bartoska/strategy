using component;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.battle.battalion.shadow;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class BattalionShadowSpawner
    {
        public static Entity spawnBattalionShadow(EntityCommandBuffer.ParallelWriter ecb, PrefabHolder prefabHolder, float3 battalionPosition, long parentBattalionId, int row, Team team)
        {
            var battalionShadowPrefab = prefabHolder.battalionShadowPrefab;
            var newBattalionShadow = ecb.Instantiate(0, battalionShadowPrefab);
            var battalionShadowMarker = new BattalionShadowMarker
            {
                parentBattalionId = parentBattalionId
            };

            var rowComponent = new Row
            {
                value = row
            };

            var teamComponent = new BattalionTeam
            {
                value = team
            };

            //todo upravit pozici podle row
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);

            ecb.AddComponent(1, newBattalionShadow, battalionShadowMarker);
            ecb.AddComponent(1, newBattalionShadow, rowComponent);
            ecb.AddComponent(1, newBattalionShadow, teamComponent);

            ecb.SetComponent(1, newBattalionShadow, battalionTransform);

            return newBattalionShadow;
        }
    }
}