using System;
using System.Collections.Generic;
using System.Linq;
using _Monobehaviors.ui.battle_plan.army_card;
using _Monobehaviors.ui.battle_plan.battle_grid;
using _Monobehaviors.ui.battle_plan.buttons;
using component;
using component.config.game_settings;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace _Monobehaviors.ui.battle_plan.counter
{
    public class ArmyFormationManager : MonoBehaviour
    {
        public static ArmyFormationManager instance;
        private List<ButtonDropTarget> allButtonDropTargets = new();
        private SoldierType? selectedType;
        private Dictionary<SoldierType, List<BattalionToSpawn>> team1 = new();
        private List<BattalionToSpawn> team2 = new();

        private void Awake()
        {
            instance = this;
        }

        public void prepare(NativeArray<BattalionToSpawn> battalions)
        {
            team2.Clear();
            team1.Clear();
            foreach (var battalion in battalions)
            {
                switch (battalion.team)
                {
                    case Team.TEAM1:
                        addTeam1Battalion(battalion);
                        break;
                    case Team.TEAM2:
                        team2.Add(battalion);
                        break;
                    default:
                        throw new Exception("Unknown team " + battalion.team);
                }
            }

            allButtonDropTargets.Clear();
            GridSpawner.instance.spawn();
            CardManager.instance.spawn(team1);
            updateSelectedType(team1.Keys.First());
        }

        private void addTeam1Battalion(BattalionToSpawn battalion)
        {
            if (team1.TryGetValue(battalion.armyType, out var battalionList))
            {
                battalionList.Add(battalion);
            }
            else
            {
                team1.Add(battalion.armyType, new List<BattalionToSpawn> {battalion});
            }
        }

        public void add(ButtonDropTarget buttonDropTarget)
        {
            allButtonDropTargets.Add(buttonDropTarget);
        }

        public NativeList<BattalionToSpawn> getAllBattalions()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.TempJob);
            //result.AddRange(prepareTeam2Positions());
            result.AddRange(spawnTeam2InMiddle());
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

        //todo remove me
        private NativeList<BattalionToSpawn> spawnTeam2InMiddle()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.TempJob);
            var i = -1;
            var j = 4;
            foreach (var battalion in team2)
            {
                var tmp = battalion;
                tmp.position = new int2(i, j++);
                result.Add(tmp);
                if (j == 10)
                {
                    j = 0;
                    i--;
                }
            }

            return result;
        }


        private NativeList<BattalionToSpawn> prepareTeam2Positions()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.TempJob);
            var i = -1;
            var j = 0;
            foreach (var battalion in team2)
            {
                var tmp = battalion;
                tmp.position = new int2(i, j++);
                result.Add(tmp);
                if (j == 10)
                {
                    j = 0;
                    i--;
                }
            }

            return result;
        }

        public BattalionToSpawn? tryToGetBattalion()
        {
            if (team1.TryGetValue(selectedType.Value, out var battalions))
            {
                var count = battalions.Count;
                if (count != 0)
                {
                    var battalion = battalions[count - 1];
                    battalions.RemoveAt(count - 1);
                    CardManager.instance.updateCard(selectedType.Value, count - 1);
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
            team1.TryGetValue(battalion.armyType, out var battalions);
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