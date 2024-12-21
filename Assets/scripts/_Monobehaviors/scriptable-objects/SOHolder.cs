using _Monobehaviors.scriptable_objects.battle;
using component.config.game_settings;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Monobehaviors.scriptable_objects
{
    public class SOHolder : MonoBehaviour
    {
        public static SOHolder instance;
        public BattleCompositionSo debugBattleCompositionSO;
        public DebugConfigSO debugConfigSO;

        private void Awake()
        {
            instance = this;
        }

        public void saveBattalionsToSpawn(DynamicBuffer<BattalionToSpawn> battalions)
        {
            var result = debugBattleCompositionSO.battalions;
            debugBattleCompositionSO.battalions.Clear();
            foreach (var battalion in battalions)
            {
                if (!battalion.position.HasValue) continue;

                var updatedBattalion = battalion;
                updatedBattalion.positionForSO = positionToSoPosition(battalion.position.Value);
                result.Add(updatedBattalion);
            }
        }

        private BattalionPosition positionToSoPosition(float3 position)
        {
            return new BattalionPosition
            {
                x = position.x,
                y = position.y,
                z = position.z
            };
        }

        public NativeList<BattalionToSpawn> getBattalionsToSpawn()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.Persistent);
            foreach (var battalion in debugBattleCompositionSO.battalions)
            {
                result.Add(new BattalionToSpawn
                {
                    battalionId = battalion.battalionId,
                    armyCompanyId = battalion.armyCompanyId,
                    armyType = battalion.armyType,
                    team = battalion.team,
                    count = battalion.count,
                    isUsed = battalion.isUsed,
                    position = new float3(battalion.positionForSO.x, battalion.positionForSO.y, battalion.positionForSO.z)
                });
            }

            return result;
        }
    }
}