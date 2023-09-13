using System;
using System.Collections.Generic;
using component.config.game_settings;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.strategy._init_map
{
    public class SpawnArmyAuthoring : MonoBehaviour
    {
        public Team team;
        public List<SpawnArmyCompany> companies = new();
    }

    [Serializable]
    public class SpawnArmyCompany
    {
        public SoldierType type;
        public int soldierCount;
    }

    public struct SpawnArmyCompanyBuffer : IBufferElementData
    {
        public SoldierType type;
        public int soldierCount;
    }

    public struct SpawnArmy : IComponentData, IEquatable<SpawnArmy>
    {
        public Team team;
        public float3 position;

        public bool Equals(SpawnArmy other)
        {
            return team.Equals(other.team) && position.Equals(other.position);
        }
    }

    public class SpawnArmyBaker : Baker<SpawnArmyAuthoring>
    {
        public override void Bake(SpawnArmyAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpawnArmy
            {
                team = authoring.team,
                position = authoring.transform.position
            });

            var dynamicBuffer = AddBuffer<SpawnArmyCompanyBuffer>(entity);

            authoring.companies.ForEach(company =>
            {
                dynamicBuffer.Add(new SpawnArmyCompanyBuffer
                {
                    type = company.type,
                    soldierCount = company.soldierCount
                });
            });
        }
    }
}