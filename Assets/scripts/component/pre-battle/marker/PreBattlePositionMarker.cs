using Unity.Entities;
using Unity.Mathematics;

namespace component.pre_battle.marker
{
    public struct PreBattlePositionMarker : IComponentData
    {
        public PreBattleMarkerState state;
        public float2? startPosition;
        public float2? endPosition;
    }

    public enum PreBattleMarkerState
    {
        IDLE,
        RUNNING,
        FINISHED
    }
}