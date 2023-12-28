using System;
using System.Collections.Generic;
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
        private List<BattalionToSpawn> team2Battalions = new();

        private void Awake()
        {
            instance = this;
        }

        public void prepare(NativeArray<BattalionToSpawn> battalions)
        {
            team2Battalions.Clear();
            var team1 = new List<BattalionToSpawn>();
            foreach (var battalion in battalions)
            {
                switch (battalion.team)
                {
                    case Team.TEAM1:
                        team1.Add(battalion);
                        break;
                    case Team.TEAM2:
                        team2Battalions.Add(battalion);
                        break;
                    default:
                        throw new Exception("Unknown team " + battalion.team);
                }
            }

            allButtonDropTargets.Clear();
            GridSpawner.planInstance.spawnPlan();
            GridSpawner.battalionInstance.spawnBattalions(team1);
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
            foreach (var battalion in team2Battalions)
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