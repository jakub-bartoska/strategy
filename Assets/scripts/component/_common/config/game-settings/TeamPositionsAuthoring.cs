using System;
using System.Collections.Generic;
using system;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.config.game_settings
{
    public class TeamPositionsAuthoring : MonoBehaviour
    {
        public List<TeamPositionAuthoring> teamPositions = new();
        
    }

    [Serializable]
    public class TeamPositionAuthoring
    {
        public Team team;
        public float2 min;
        public float2 max;
    }

    public struct TeamPositions : IBufferElementData
    {
        public Team team;
        public float2 min;
        public float2 max;
    }

    public class TeamPositionsBaker : Baker<TeamPositionsAuthoring>
    {
        public override void Bake(TeamPositionsAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);

            var dynamicBuffer = AddBuffer<TeamPositions>(entity);

            authoring.teamPositions.ForEach(position =>
            {
                dynamicBuffer.Add(new TeamPositions
                {
                    team = position.team,
                    min = position.min,
                    max = position.max
                });
            });
        }
    }
}