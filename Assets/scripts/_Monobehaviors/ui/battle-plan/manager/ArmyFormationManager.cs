using System;
using System.Collections.Generic;
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
                        addTeam1Batalion(battalion);
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
        }

        private void addTeam1Batalion(BattalionToSpawn battalion)
        {
            if (team1.TryGetValue(battalion.armyType, out var battalionList))
            {
                battalionList.Add(battalion);
            }
            else
            {
                team1.Add(battalion.armyType, new List<BattalionToSpawn> { battalion });
            }
        }

        public void add(ButtonDropTarget buttonDropTarget)
        {
            allButtonDropTargets.Add(buttonDropTarget);
        }


        public NativeList<BattalionToSpawn> getAllBatalions()
        {
            var result = new NativeList<BattalionToSpawn>(Allocator.TempJob);
            result.AddRange(prepareTeam2Positions());
            foreach (var allButtonDropTarget in allButtonDropTargets)
            {
                var batalion = allButtonDropTarget.getBatalion();
                if (batalion.HasValue)
                {
                    result.Add(batalion.Value);
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
    }
}