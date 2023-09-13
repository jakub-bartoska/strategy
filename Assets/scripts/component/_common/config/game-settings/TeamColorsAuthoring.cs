using System;
using System.Collections.Generic;
using system;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component.config.game_settings
{
    public class TeamColorsAuthoring : MonoBehaviour
    {
        public List<TeamColorAuthoring> teamColors = new();
    }

    [Serializable]
    public class TeamColorAuthoring
    {
        public Team team;
        public float3 color;
    }

    public struct TeamColor : IBufferElementData
    {
        public Team team;
        public float4 color;
    }

    public class TeamColorBaker : Baker<TeamColorsAuthoring>
    {
        public override void Bake(TeamColorsAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);

            var dynamicBuffer = AddBuffer<TeamColor>(entity);

            authoring.teamColors.ForEach(color =>
            {
                dynamicBuffer.Add(new TeamColor
                {
                    team = color.team,
                    color = new float4(color.color.x, color.color.y, color.color.z, 0)
                });
            });
        }
    }
}