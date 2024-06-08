using System;
using System.Collections.Generic;
using System.Linq;
using _Monobehaviors.scriptable_objects.battle;
using _Monobehaviors.ui.battle_plan.army_card;
using _Monobehaviors.ui.battle_plan.battle_grid;
using _Monobehaviors.ui.battle_plan.buttons;
using component;
using component.config.game_settings;
using Unity.Collections;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.counter
{
    public class ArmyFormationManager : MonoBehaviour
    {
        public static ArmyFormationManager instance;
        [SerializeField] private BattleCompositionSo battleComposition;
        private List<ButtonDropTarget> allButtonDropTargets = new();
        private NativeArray<BattalionToSpawn> battalionsBackup;
        private SoldierType selectedType = SoldierType.SWORDSMAN;
        private Dictionary<SoldierType, List<BattalionToSpawn>> team1 = new();
        private Dictionary<SoldierType, List<BattalionToSpawn>> team2 = new();

        private void Awake()
        {
            instance = this;
        }

        public void prepare(NativeArray<BattalionToSpawn> battalions)
        {
            battalionsBackup = battalions;

            team2.Clear();
            team1.Clear();
            allButtonDropTargets.Clear();
            foreach (var battalion in battalions)
            {
                addTeamBattalion(battalion);
            }

            CardManager.instance.spawn(team1);
            GridSpawner.instance.spawn(battleComposition.battalions);
            updateSelectedType(team1.Keys.First());
        }

        private void addTeamBattalion(BattalionToSpawn battalion)
        {
            var source = battalion.team switch
            {
                Team.TEAM1 => team1,
                Team.TEAM2 => team2,
                _ => throw new Exception("Unknown team")
            };
            if (source.TryGetValue(battalion.armyType, out var battalionList))
            {
                battalionList.Add(battalion);
            }
            else
            {
                source.Add(battalion.armyType, new List<BattalionToSpawn> {battalion});
            }
        }

        public void add(ButtonDropTarget buttonDropTarget)
        {
            allButtonDropTargets.Add(buttonDropTarget);
        }

        public void clear()
        {
            team2.Clear();
            team1.Clear();
            allButtonDropTargets.ForEach(button => Destroy(button.gameObject));
            allButtonDropTargets.Clear();
            battleComposition.battalions.Clear();
            prepare(battalionsBackup);
        }

        public NativeList<BattalionToSpawn> getAllBattalions()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.TempJob); //ok
            foreach (var allButtonDropTarget in allButtonDropTargets)
            {
                var battalion = allButtonDropTarget.getBattalion();
                if (battalion.HasValue)
                {
                    result.Add(battalion.Value);
                }
            }

            return result;
        }

        public BattalionToSpawn? tryToGetBattalion(Team team)
        {
            var source = team switch
            {
                Team.TEAM1 => team1,
                Team.TEAM2 => team2,
                _ => throw new Exception("Unknown team")
            };
            if (source.TryGetValue(selectedType, out var battalions))
            {
                var count = battalions.Count;
                if (count != 0)
                {
                    var battalion = battalions[count - 1];
                    battalions.RemoveAt(count - 1);
                    CardManager.instance.updateCard(selectedType, count - 1);
                    redrawStartButton();
                    return battalion;
                }
            }

            return null;
        }

        private void redrawStartButton()
        {
            var totalCount = 0;
            foreach (var list in team1.Values)
            {
                totalCount += list.Count;
            }

            StartBattleButton.instance.updateActivity(totalCount != 0 ? CardState.INACTIVE : CardState.ACTIVE);
        }

        public void returnBattalion(BattalionToSpawn battalion)
        {
            var source = battalion.team switch
            {
                Team.TEAM1 => team1,
                Team.TEAM2 => team2,
                _ => throw new Exception("Unknown team")
            };
            source.TryGetValue(battalion.armyType, out var battalions);
            battalions.Add(battalion);
            CardManager.instance.updateCard(battalion.armyType, battalions.Count);
            redrawStartButton();
        }

        public void updateSelectedType(SoldierType type)
        {
            selectedType = type;
            CardManager.instance.updateCardColors(type);
        }
    }
}