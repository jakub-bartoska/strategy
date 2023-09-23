using System;
using System.Collections.Generic;
using _Monobehaviors.ui.player_resources;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.strategy.minor_objects
{
    public class MinorMapObjectAuthoring : MonoBehaviour
    {
        public Team team;
        public MinorObjectType type;
        public List<SpawnResourceGenerator> resourceGenerators = new();
    }

    public enum MinorObjectType
    {
        MILL,
        GOLD_MINE,
        STONE_MINE,
        LUMBERJACK_HUT
    }

    public struct SpawnMinorMapObject : IComponentData, IEquatable<SpawnMinorMapObject>
    {
        public MinorObjectType type;
        public Team team;
        public float3 position;

        public bool Equals(SpawnMinorMapObject other)
        {
            return team.Equals(other.team) && position.Equals(other.position) && type.Equals(other.type);
        }
    }

    public struct MinorMapObject : IComponentData
    {
        public MinorObjectType type;
    }

    [Serializable]
    public struct SpawnResourceGenerator : IBufferElementData
    {
        public ResourceType type;
        public int value;
    }

    public class SpawnMinorMapObjectBaker : Baker<MinorMapObjectAuthoring>
    {
        public override void Bake(MinorMapObjectAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnMinorMapObject
            {
                team = authoring.team,
                type = authoring.type,
                position = authoring.transform.position
            });
            var buffer = AddBuffer<SpawnResourceGenerator>();
            authoring.resourceGenerators.ForEach(rg => buffer.Add(rg));
        }
    }
}