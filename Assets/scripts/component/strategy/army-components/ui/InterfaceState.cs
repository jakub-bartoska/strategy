using Unity.Entities;

namespace component.strategy.army_components.ui
{
    public struct InterfaceState : IComponentData
    {
        public UIState state;
        public UIState oldState;
    }

    public enum UIState
    {
        ALL_CLOSED,
        TOWN_UI,
        ARMY_UI,
        MINOR_UI,
        GET_NEW_STATE
    }
}