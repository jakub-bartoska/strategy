using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Monobehaviors.ui_toolkit.pre_battle
{
    public class StartButtonGroup : MonoBehaviour
    {
        public static StartButtonGroup instance;
        private EntityManager entityManager;
        private VisualElement root;

        private void Awake()
        {
            instance = this;
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        /*
        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            uiStateQuery = entityManager.CreateEntityQuery(typeof(PreBattleUiState));
        }

        private void onTeamButtonClicked(Team team)
        {
            var uiState = uiStateQuery.GetSingletonRW<PreBattleUiState>();
            uiState.ValueRW.selectedTeam = team;
            uiState.ValueRW.preBattleEvent = PreBattleEvent.TEAM_CHANGED;
        }

        public void updateTeamButtons(PreBattleUiState state)
        {
            updateButtonStyles(state.selectedTeam);
        }

        private void updateButtonStyles(Team selectedTeam)
        {
            switch (selectedTeam)
            {
                case Team.TEAM1:
                    team1Button.AddToClassList("team-button-team1-selected");
                    team2Button.RemoveFromClassList("team-button-team2-selected");
                    break;
                case Team.TEAM2:
                    team2Button.AddToClassList("team-button-team2-selected");
                    team1Button.RemoveFromClassList("team-button-team1-selected");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        */
    }
}