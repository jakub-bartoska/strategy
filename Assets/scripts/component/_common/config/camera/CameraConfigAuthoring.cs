using System;
using System.Collections.Generic;
using component._common.system_switchers;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace component._common.config.camera
{
    public class CameraConfigAuthoring : MonoBehaviour
    {
        public List<CameraSettings> CameraSettings;
    }

    [Serializable]
    public class CameraSettings
    {
        public SystemStatus gameCameraType;
        public float3 minValues;
        public float3 maxValues;
    }

    public class CameraConfigAuthoringBaker : Baker<CameraConfigAuthoring>
    {
        public override void Bake(CameraConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var dynamicBuffer = AddBuffer<CameraConfigComponentData>(entity);

            authoring.CameraSettings.ForEach(settings =>
            {
                dynamicBuffer.Add(new CameraConfigComponentData
                {
                    maxValues = settings.maxValues,
                    minValues = settings.minValues,
                    gameCameraType = settings.gameCameraType
                });
            });
        }
    }

    public struct CameraConfigComponentData : IBufferElementData
    {
        public SystemStatus gameCameraType;
        public float3 minValues;
        public float3 maxValues;
    }
}