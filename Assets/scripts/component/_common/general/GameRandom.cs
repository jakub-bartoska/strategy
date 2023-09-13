using Unity.Entities;
using Unity.Mathematics;

namespace component
{
    public struct GameRandom : IComponentData
    {
        public Random random;
    }
}