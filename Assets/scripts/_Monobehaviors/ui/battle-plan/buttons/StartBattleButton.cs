using _Monobehaviors.ui.battle_plan.counter;
using component._common.system_switchers;
using component.config.game_settings;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.buttons
{
    public class StartBattleButton : MonoBehaviour
    {
        private EntityQuery batalionToSpawn;
        private EntityManager entityManager;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            batalionToSpawn = entityManager.CreateEntityQuery(typeof(BattalionToSpawn));
        }

        public void onStartBattle()
        {
            var battalions = ArmyFormationManager.instance.getAllBatalions();
            var buffer = batalionToSpawn.GetSingletonBuffer<BattalionToSpawn>();
            buffer.Clear();
            buffer.AddRange(battalions);

            StateManagerForMonos.getInstance().updateStatusFromMonos(SystemStatus.BATTLE);
        }
    }
}