using System;
using Unity.Entities;

namespace component.battle.battalion
{
    public struct BattalionSoldiers : IBufferElementData, IEquatable<BattalionSoldiers>
    {
        public long soldierId;
        public Entity entity;
        public int positionWithinBattalion;

        public bool Equals(BattalionSoldiers other)
        {
            return soldierId == other.soldierId;
        }
    }
}