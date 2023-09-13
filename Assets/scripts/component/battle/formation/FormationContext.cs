using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.formation
{
    public struct FormationContext : IComponentData
    {
        public int id;
        public FormationType formationType;
        public int formationSize;
        public NativeHashMap<int, int> soldierIdToFormationIndex;
        public float distanceBetweenSoldiers;
        public float3 formationCenter;
    }

    public enum FormationType
    {
        LINE
    }
}