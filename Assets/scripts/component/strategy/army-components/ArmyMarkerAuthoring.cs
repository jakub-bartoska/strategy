using Unity.Entities;
using UnityEngine;

namespace component.strategy.army_components
{
    public class ArmyMarkerAuthoring : MonoBehaviour
    {
    }

    public struct ArmyMarkerTag : IComponentData
    {
    }

    public class ArmyMarkerAuthoringBaker : Baker<ArmyMarkerAuthoring>
    {
        public override void Bake(ArmyMarkerAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArmyMarkerTag());
        }
    }
}