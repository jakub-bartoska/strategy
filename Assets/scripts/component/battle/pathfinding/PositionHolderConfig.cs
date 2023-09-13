using System.Drawing;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace component.pathfinding
{
    public struct PositionHolderConfig : IComponentData
    {
        public int oneSquareSize;
        public int2 minSquarePosition;
        public int2 maxSquarePosition;
    }
}