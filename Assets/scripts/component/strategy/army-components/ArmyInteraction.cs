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
        MERGE_TOGETHER,
        FIGHT,
        MERGE_ME_INTO,
        ENTER_TOWN,
        FIGHT_TOWN,
        ANY_ARMY_WITH_ARMY_INTERACTION
    }
}