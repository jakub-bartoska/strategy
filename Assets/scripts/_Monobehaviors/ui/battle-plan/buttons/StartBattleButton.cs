using System;
using _Monobehaviors.scriptable_objects.battle;
using _Monobehaviors.ui.battle_plan.army_card;
using _Monobehaviors.ui.battle_plan.counter;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class StartBattleButton : MonoBehaviour
    {
        public static StartBattleButton instance;
        [SerializeField] private BattleCompositionSo battleComposition;
        private Color active = new(0.1627803f, 0.5849056f, 0.2474526f);
        private EntityQuery battalionToSpawn;
        private EntityManager entityManager;
        private Image image;
        private Color inactive = new(0.6132076f, 0.6132076f, 0.6132076f);
        private CardState state;

        private void Awake()
        {
            state = CardState.INACTIVE;
            instance = this;
            image = GetComponent<Image>();
        }

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            battalionToSpawn = entityManager.CreateEntityQuery(typeof(BattalionToSpawn));
        }

        public void onStartBattle()
        {
            var battalions = ArmyFormationManager.instance.getAllBattalions();
            var buffer = battalionToSpawn.GetSingletonBuffer<BattalionToSpawn>();
            buffer.Clear();
            buffer.AddRange(battalions.AsArray());

            battleComposition.battalions.Clear();
            foreach (var toSpawn in battalions)
            {
                battleComposition.battalions.Add(toSpawn);
            }

            battalions.Dispose();
            StateManagerForMonos.getInstance().updateStatusFromMonos(SystemStatus.BATTLE);
        }

        public void updateActivity(CardState newState)
        {
            if (state == newState) return;

            state = newState;

            switch (state)
            {
                case CardState.ACTIVE:
                    image.color = active;
                    break;
                case CardState.INACTIVE:
                    image.color = inactive;
                    break;
                default:
                    throw new Exception("Unknown state " + state + " in StartBattleButton");
            }
        }
    }
}