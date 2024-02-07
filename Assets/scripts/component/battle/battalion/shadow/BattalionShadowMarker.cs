using Unity.Entities;

namespace component.battle.battalion.shadow
{
    public struct BattalionShadowMarker : IComponentData
    {
        public long parentBattalionId;
    }
}