using System;
using component;
using component.authoring_pairs.PrefabHolder;
using component.battle.battalion;
using component.config.game_settings;
using component.general;
using component.soldier;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class SoldierSpawner
    {
        public static Entity spawnSoldier(SoldierType soldierType, PrefabHolder prefabHolder, EntityCommandBuffer.ParallelWriter ecb,
            int index, int entityIndexAdd, long companyId, Team team, float3 battalionPosition,
            NativeParallelHashSet<BattalionSoldiers>.ParallelWriter battalionSoldiers, float4 teamColor)
        {
            Entity prefab = soldierType switch
            {
                SoldierType.ARCHER => prefabHolder.archerPrefab,
                SoldierType.SWORDSMAN => prefabHolder.soldierPrefab,
                _ => throw new Exception("unknown soldier type")
            };

            var newEntity = ecb.Instantiate(index, prefab);
            var calculatedPosition = getPosition(index, battalionPosition);
            var transform = LocalTransform.FromPosition(calculatedPosition);

            var soldierStats = new SoldierStatus
            {
                index = index + entityIndexAdd,
                team = team,
                companyId = companyId
            };
            var soldierHp = new SoldierHp
            {
                hp = 100
            };

            var color = new MaterialColorComponent
            {
                Value = teamColor
            };

            ecb.SetName(index, newEntity, "Soldier " + soldierStats.index);

            //add components
            ecb.AddComponent(index, newEntity, soldierStats);
            ecb.AddComponent(index, newEntity, soldierHp);
            ecb.AddComponent(index, newEntity, color);
            ecb.AddComponent(index, newEntity, new BattleCleanupTag());

            //set component
            ecb.SetComponent(index, newEntity, transform);

            battalionSoldiers.Add(new BattalionSoldiers
            {
                soldierId = soldierStats.index,
                entity = newEntity,
                positionWithinBattalion = index
            });

            return newEntity;
        }

        private static float3 getPosition(int index, float3 battalionPosition)
        {
            var soldierWithinBattalionPosition = new float3
            {
                x = 0,
                y = 0,
                z = index + 0.5f
            };
            return battalionPosition + soldierWithinBattalionPosition;
        }
    }
}