using _Monobehaviors.debug;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace system._common.debug
{
    public partial struct FpsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            float fps = 1.0f / deltaTime;
            FpsMonobehavior.instance.updateFps((int) fps);
        }
    }
}