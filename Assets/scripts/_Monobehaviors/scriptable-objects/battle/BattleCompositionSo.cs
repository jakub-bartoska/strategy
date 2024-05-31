using System.Collections.Generic;
using component.config.game_settings;
using UnityEngine;

namespace _Monobehaviors.scriptable_objects.battle
{
    [CreateAssetMenu(fileName = "Battle-composition", menuName = "ScriptableObjects/DebugBattleCompositionSO")]
    public class BattleCompositionSo : ScriptableObject
    {
        public List<BattalionToSpawn> battalions = new();
    }
}