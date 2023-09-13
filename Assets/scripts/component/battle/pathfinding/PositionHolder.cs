using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.helpers.positioning
{
    public struct PositionHolder : IComponentData
    {
        public NativeParallelMultiHashMap<int, float3> soldierIdPosition;
        public NativeParallelMultiHashMap<int2, int> team1PositionCells;
        public NativeParallelMultiHashMap<int2, int> team2PositionCells;
    }
}