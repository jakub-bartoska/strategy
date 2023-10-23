using component.config.game_settings;
using component.strategy.buy_army;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui.soldier_buy
{
    public class BuyButton : MonoBehaviour
    {
        private EntityManager entityManager;
        private EntityQuery townDeployQuery;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            townDeployQuery = entityManager.CreateEntityQuery(typeof(ArmyPurchase));
        }

        public void buySwordsman(int count)
        {
            buySoldiers(count, SoldierType.SWORDSMAN);
        }

        public void buyArcher(int count)
        {
            buySoldiers(count, SoldierType.ARCHER);
        }

        public void buyCavalry(int count)
        {
            buySoldiers(count, SoldierType.HORSEMAN);
        }

        private void buySoldiers(int count, SoldierType soldierType)
        {
            var townDeployBuffer = townDeployQuery.GetSingletonBuffer<ArmyPurchase>();
            townDeployBuffer.Add(new ArmyPurchase
                {
                    type = soldierType,
                    count = count
                }
            );
        }
    }
}