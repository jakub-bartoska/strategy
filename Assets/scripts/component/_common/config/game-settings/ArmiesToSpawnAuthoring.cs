using System;
using System.Collections.Generic;
using component.strategy.general;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.config.game_settings
{
    public class ArmiesToSpawnAuthoring : MonoBehaviour
    {
        public List<CompanyToSpawnAuthoring> armies = new();
    }

    [Serializable]
    public class CompanyToSpawnAuthoring
    {
        public Team team;
        public SoldierType armyType;
        public int count;
        public float distanceBetweenSoldiers;
    }

    public struct CompanyToSpawn : IBufferElementData
    {
        public long originalArmyId;
        public HolderType originalArmyType;
        public long armyCompanyId;
        public Team team;
        public SoldierType armyType;
        public int count;
    }

    [Serializable]
    public struct BattalionToSpawn : IBufferElementData
    {
        public long armyCompanyId;
        public Team team;
        public SoldierType armyType;
        public int count;
        public int2 position;
    }

    public struct CompanyToSpawnMono : IBufferElementData
    {
        public Team team;
        public SoldierType armyType;
        public int count;
        public float distanceBetweenSoldiers;
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
            var dynamicBuffer = AddBuffer<CompanyToSpawnMono>(entity);

            authoring.armies.ForEach(army =>
            {
                dynamicBuffer.Add(new CompanyToSpawnMono
                {
                    team = army.team,
                    armyType = army.armyType,
                    count = army.count,
                    distanceBetweenSoldiers = army.distanceBetweenSoldiers
                });
            });
        }
    }
}