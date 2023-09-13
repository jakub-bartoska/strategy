using System;
using System.Collections.Generic;
using component.strategy.general;
using Unity.Entities;
using UnityEngine;

namespace component.config.game_settings
{
    public class ArmiesToSpawnAuthoring : MonoBehaviour
    {
        public List<ArmyToSpawnAuthoring> armies = new();
    }

    [Serializable]
    public class ArmyToSpawnAuthoring
    {
        public Team team;
        public SoldierType armyType;
        public int count;
        public Formation formation;
        public float distanceBetweenSoldiers;
    }

    public struct ArmyToSpawn : IBufferElementData
    {
        public long originalArmyId;
        public HolderType originalArmyType;
        public long armyCompanyId;
        public Team team;
        public SoldierType armyType;
        public int count;
        public Formation formation;
        public float distanceBetweenSoldiers;
    }

    public struct ArmyToSpawnMono : IBufferElementData
    {
        public Team team;
        public SoldierType armyType;
        public int count;
        public Formation formation;
        public float distanceBetweenSoldiers;
    }

    public enum Formation
    {
        NO_FORMATION,
        SQUERE,
        LINE
    }

    public enum SoldierType
    {
        ARCHER,
        SWORDSMAN,
        HORSEMAN
    }

    public class ArmiesToSpawnBaker : Baker<ArmiesToSpawnAuthoring>
    {
        public override void Bake(ArmiesToSpawnAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            var dynamicBuffer = AddBuffer<ArmyToSpawnMono>(entity);

            authoring.armies.ForEach(army =>
            {
                dynamicBuffer.Add(new ArmyToSpawnMono
                {
                    team = army.team,
                    armyType = army.armyType,
                    count = army.count,
                    formation = army.formation,
                    distanceBetweenSoldiers = army.distanceBetweenSoldiers
                });
            });
        }
    }
}