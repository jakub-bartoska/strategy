using component.strategy.buildings;
using component.strategy.buy_army;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.town_buildings_ui.town_buy
{
    public class BuildingBuyer : MonoBehaviour
    {
        public static BuildingBuyer instance;
        private EntityQuery buildingPurchaseQuery;
        private EntityManager entityManager;

        private void Awake()
        {
            instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            buildingPurchaseQuery = entityManager.CreateEntityQuery(typeof(BuildingPurchase));
        }

        public void buyBuilding(BuildingType buildingType)
        {
            var buildingPurchases = buildingPurchaseQuery.GetSingletonBuffer<BuildingPurchase>();
            buildingPurchases.Add(new BuildingPurchase
                {
                    type = buildingType
                }
            );
        }
    }
}