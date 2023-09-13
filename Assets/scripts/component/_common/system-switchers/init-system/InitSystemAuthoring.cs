using Unity.Entities;
using UnityEngine;

namespace component._common.system_switchers
{
    public class InitSystemAuthoring : MonoBehaviour
    {
        public SystemStatus desiredStatus;
    }

    public class InitSystemBaker : Baker<InitSystemAuthoring>
    {
        public override void Bake(InitSystemAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(entity, new InitState
            {
                desiredStatus = authoring.desiredStatus
            });
        }
    }
}