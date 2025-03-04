﻿using System;
using component;
using component._common.camera;
using component._common.general;
using component._common.movement_agents;
using component._common.system_switchers;
using component.config.game_settings;
using component.helpers;
using component.pre_battle;
using component.pre_battle.cards;
using component.pre_battle.marker;
using component.strategy.buy_army;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace system._common
{
    public partial struct CommonEntitiesSpawner : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var singletonEntity = ecb.CreateEntity();

            var random = new GameRandom
            {
                random = Random.CreateFromIndex(10)
            };

            var battalionIdGenerator = new BattalionIdGenerator
            {
                nextBattalionIdToBeUsed = 0
            };

            var systemSwitcherMarker = new SystemStatusHolder
            {
                currentStatus = SystemStatus.NO_STATUS,
                desiredStatus = SystemStatus.NO_STATUS,
                previousStatus = SystemStatus.NO_STATUS
            };
            var preBattlePositionMarker = new PreBattlePositionMarker
            {
                state = PreBattleMarkerState.IDLE
            };
            var strategyCamera = new StrategyCamera
            {
                desiredPosition = new float3(-10, 10, -13)
            };
            var battleCamera = new BattleCamera()
            {
                desiredPosition = new float3(10000, 100, 9950)
            };
            var prebattleUiState = new PreBattleUiState
            {
                selectedTeam = Team.TEAM1,
                selectedCard = SoldierType.ARCHER,
                preBattleEvent = PreBattleEvent.INIT
            };

            ecb.AddComponent(singletonEntity, battalionIdGenerator);
            ecb.AddComponent(singletonEntity, preBattlePositionMarker);
            ecb.AddComponent(singletonEntity, battleCamera);
            ecb.AddComponent(singletonEntity, strategyCamera);
            ecb.AddComponent(singletonEntity, random);
            ecb.AddComponent(singletonEntity, systemSwitcherMarker);
            ecb.AddComponent(singletonEntity, prebattleUiState);
            ecb.AddComponent(singletonEntity, new SingletonEntityTag());
            ecb.AddComponent(singletonEntity, new AgentMovementAllowedTag());
            ecb.AddComponent(singletonEntity, new AgentMovementAllowedForBattleTag());
            ecb.AddBuffer<SystemSwitchBlocker>(singletonEntity);
            ecb.AddBuffer<ArmyPurchase>(singletonEntity);
            ecb.AddBuffer<BuildingPurchase>(singletonEntity);
            ecb.AddBuffer<CompanyToSpawn>(singletonEntity);
            ecb.AddBuffer<BattalionToSpawn>(singletonEntity);
            ecb.AddBuffer<Damage>(singletonEntity);
            ecb.AddBuffer<PreBattleBattalion>(singletonEntity);
            var cards = ecb.AddBuffer<CardInfo>(singletonEntity);
            addCards(cards);
        }

        private void addCards(DynamicBuffer<CardInfo> cards)
        {
            foreach (Team team in Enum.GetValues(typeof(Team)))
            {
                foreach (SoldierType soldierType in Enum.GetValues(typeof(SoldierType)))
                {
                    cards.Add(new CardInfo
                    {
                        team = team,
                        soldierType = soldierType,
                        maxBattalionCount = 0
                    });
                }
            }
        }
    }
}