using _Monobehaviors.ui_toolkit.pre_battle;
using component;
using component._common.system_switchers;
using component.config.game_settings;
using component.pre_battle.cards;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.pre_battle
{
    public partial struct CardCountCalculatorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PreBattleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cardInfos = SystemAPI.GetSingletonBuffer<CardInfo>();
            var battalionsToSpawn = SystemAPI.GetSingletonBuffer<BattalionToSpawn>();

            var battalionCounter = new NativeHashMap<(Team, SoldierType), int>(cardInfos.Length, Allocator.TempJob);
            foreach (var battalionToSpawn in battalionsToSpawn)
            {
                if (battalionToSpawn.position.HasValue)
                {
                    continue;
                }

                var key = (battalionToSpawn.team, battalionToSpawn.armyType);
                if (battalionCounter.TryGetValue(key, out var count))
                {
                    battalionCounter[key] = count + 1;
                }
                else
                {
                    battalionCounter.Add(key, 1);
                }
            }

            var cardChanged = false;
            for (int i = 0; i < cardInfos.Length; i++)
            {
                var cardInfo = cardInfos[i];
                var key = (cardInfo.team, cardInfo.soldierType);
                if (battalionCounter.TryGetValue(key, out var count))
                {
                    if (count == cardInfo.battalionCount) continue;

                    var newValue = cardInfo;
                    newValue.battalionCount = count;
                    cardInfos[i] = newValue;

                    cardChanged = true;
                }
            }

            if (cardChanged)
            {
                PreBattleUi.instance.updateCards(cardInfos);
            }

            battalionCounter.Dispose();
        }
    }
}