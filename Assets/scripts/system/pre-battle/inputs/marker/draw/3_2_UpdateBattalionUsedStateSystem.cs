using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle.marker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.pre_battle.inputs
{
    [UpdateAfter(typeof(FinishSystem))]
    public partial struct UpdateBattalionUsedStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cards = SystemAPI.GetSingletonBuffer<PreBattleBattalion>();
            var usedBattalionIds = new NativeHashSet<long>(cards.Length, Allocator.Temp);
            var preBattlePositionMarker = SystemAPI.GetSingleton<PreBattlePositionMarker>();

            foreach (var card in cards)
            {
                var battalionId = preBattlePositionMarker.state == PreBattleMarkerState.IDLE ? card.battalionId : card.battalionIdTmp;

                if (battalionId.HasValue)
                {
                    usedBattalionIds.Add(battalionId.Value);
                }
            }

            var battalions = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();
            for (int i = 0; i < battalions.Length; i++)
            {
                var battalion = battalions[i];
                var isUsed = usedBattalionIds.Contains(battalion.battalionId);

                if (isUsed == battalion.isUsed)
                {
                    continue;
                }

                battalions[i] = new BattalionToSpawn
                {
                    battalionId = battalion.battalionId,
                    armyCompanyId = battalion.armyCompanyId,
                    team = battalion.team,
                    armyType = battalion.armyType,
                    count = battalion.count,
                    position = battalion.position,
                    isUsed = isUsed
                };
            }
        }
    }
}