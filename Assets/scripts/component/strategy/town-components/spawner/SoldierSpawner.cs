using component.config.game_settings;
using Unity.Entities;

namespace component.strategy.town_components
{
    public struct SoldierSpawner : IComponentData
    {
        public int soldiersAmountToSpawn;
        public SoldierType soldierType;
        public float cycleTime;
        public float timeLeft;
    }
}