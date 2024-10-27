using Unity.Entities;
using Unity.Mathematics;

namespace component.pre_battle.marker
{
    public struct PreBattlePositionMarker : IComponentData
    {
        public PreBattleMarkerState state;
        public float2? startPosition;
        public float2? endPosition;
        public MarkerType MarkerType;
    }

    public enum PreBattleMarkerState
    {
        IDLE,
        INIT,
        RUNNING,
        FINISHED
    }

    public enum MarkerType
    {
        ADD,
        REMOVE
    }
}