using Unity.Entities;

namespace component.strategy.army_components
{
    public struct ArmyInteraction : IBufferElementData
    {
        public long armyId;
        public InteractionType interactionType;
    }

    public enum InteractionType
    {
        FIGHT,
        FIGHT_TOWN,
        ENTER_TOWN,
        ANY_ARMY_MERGE,
        MERGE_TOGETHER,
        MERGE_ME_INTO,
        DEFEND_MINOR,
        CAPTURE_MINOR
    }
}