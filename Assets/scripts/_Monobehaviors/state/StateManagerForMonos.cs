using System;
using component._common.system_switchers;
using Unity.Entities;

namespace _Monobehaviors.ui
{
    public class StateManagerForMonos
    {
        private static StateManagerForMonos instance;
        private EntityQuery blockersQuery;
        private EntityManager entityManager;
        private EntityQuery query;

        //new, old
        public event Action<SystemStatus, SystemStatus> onSystemStatusChanged;

        public static StateManagerForMonos getInstance()
        {
            if (instance == null)
            {
                instance = new StateManagerForMonos();
                instance.init();
            }

            return instance;
        }

        public void updateStatus(SystemStatus newStatus, SystemStatus oldStatus)
        {
            onSystemStatusChanged(newStatus, oldStatus);
        }

        public void init()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(typeof(SystemStatusHolder));
            blockersQuery = entityManager.CreateEntityQuery(typeof(SystemSwitchBlocker));
        }

        public void updateStatusFromMonos(SystemStatus status)
        {
            var blockers = blockersQuery.GetSingletonBuffer<SystemSwitchBlocker>();
            blockers.Add(new SystemSwitchBlocker
            {
                blocker = Blocker.AUTO_ADD_BLOCKERS
            });

            var systemStatusHolder = query.GetSingletonRW<SystemStatusHolder>();
            systemStatusHolder.ValueRW.desiredStatus = status;
        }

        public void updateToPreviousStatus()
        {
            var blockers = blockersQuery.GetSingletonBuffer<SystemSwitchBlocker>();
            blockers.Add(new SystemSwitchBlocker
            {
                blocker = Blocker.AUTO_ADD_BLOCKERS
            });

            var systemStatusHolder = query.GetSingletonRW<SystemStatusHolder>();
            systemStatusHolder.ValueRW.desiredStatus = systemStatusHolder.ValueRO.previousStatus;
        }
    }
}