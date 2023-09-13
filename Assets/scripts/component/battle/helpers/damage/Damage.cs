using Unity.Entities;

namespace component.helpers
{
    public struct Damage : IBufferElementData
    {
        public int dmgReceiverId;
        public int dmgAmount;
    }
}