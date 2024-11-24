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
        public static Entity spawnBattalionShadow(EntityCommandBuffer ecb, PrefabHolder prefabHolder, float3 battalionPosition, long parentBattalionId, int row, Team team,
            float size)
        {
            var battalionShadowPrefab = prefabHolder.battalionShadowPrefab;
            var newBattalionShadow = ecb.Instantiate(battalionShadowPrefab);
            var battalionShadowMarker = new BattalionShadowMarker
            {
                parentBattalionId = parentBattalionId
            };
            var battleUnitType = new BattleUnitType
            {
                id = parentBattalionId,
                type = BattleUnitTypeEnum.SHADOW
            };
            var rowComponent = new Row
            {
                value = row
            };

            var teamComponent = new BattalionTeam
            {
                value = team
            };
            var battalionSize = new BattalionWidth
            {
                value = size
            };

            var transformMatrix = BattalionSpawner.getPostTransformMatrixFromBattalionSize(size);

            battalionPosition.z = CustomTransformUtils.getBattalionZPosition(row, 10);
            var battalionTransform = LocalTransform.FromPosition(battalionPosition);

            ecb.AddComponent(newBattalionShadow, battalionShadowMarker);
            ecb.AddComponent(newBattalionShadow, rowComponent);
            ecb.AddComponent(newBattalionShadow, teamComponent);
            ecb.AddComponent(newBattalionShadow, transformMatrix);
            ecb.AddComponent(newBattalionShadow, battalionSize);
            ecb.AddComponent(newBattalionShadow, battleUnitType);

            ecb.SetComponent(newBattalionShadow, battalionTransform);

            return newBattalionShadow;
        }
    }
}