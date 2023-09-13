using component.strategy.events;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui
{
    public class DeployArmy : MonoBehaviour
    {
        private EntityManager entityManager;
        private EntityQuery townDeployQuery;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            townDeployQuery = entityManager.CreateEntityQuery(typeof(CreateNewArmyEvent));
        }

        public void onClick()
        {
            var companiesToDeploy = TownUi.instance.getCompaniesReadyToDeploy();
            var townDeployBuffer = townDeployQuery.GetSingletonBuffer<CreateNewArmyEvent>();
            townDeployBuffer.Add(new CreateNewArmyEvent
                {
                    companiesToDeploy = companiesToDeploy
                }
            );
        }
    }
}