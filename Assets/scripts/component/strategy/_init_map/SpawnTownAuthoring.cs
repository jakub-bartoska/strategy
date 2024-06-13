using System;
using System.Collections.Generic;
using component.config.game_settings;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.strategy._init_map
{
    public class SpawnTownAuthoring : MonoBehaviour
    {
        public Team team;
        public List<SpawnTownCompany> companies = new();
    }

    [Serializable]
    public class SpawnTownCompany
    {
        public SoldierType type;
        public int soldierCount;
    }

    public struct SpawnTownCompanyBuffer : IBufferElementData
    {
        public SoldierType type;
        public int soldierCount;
    }

    public struct SpawnTown : IComponentData, IEquatable<SpawnTown>
    {
        public Team team;
        public float3 position;

        public bool Equals(SpawnTown other)
        {
            return ((int) team).Equals((int) other.team) && position.Equals(other.position);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) team, position);
        }
    }

    public class SpawnTownBaker : Baker<SpawnTownAuthoring>
    {
        public override void Bake(SpawnTownAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpawnTown
            {
                team = authoring.team,
                position = authoring.transform.position
            });

            var dynamicBuffer = AddBuffer<SpawnTownCompanyBuffer>(entity);

            authoring.companies.ForEach(company =>
            {
                dynamicBuffer.Add(new SpawnTownCompanyBuffer
                {
                    type = company.type,
                    soldierCount = company.soldierCount
                });
            });
        }
    }
}