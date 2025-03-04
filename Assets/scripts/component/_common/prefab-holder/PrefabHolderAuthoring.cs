﻿using Unity.Entities;
using UnityEngine;

namespace component.authoring_pairs.PrefabHolder
{
    public class PrefabHolderAuthoring : MonoBehaviour
    {
        public GameObject soldierPrefab;
        public GameObject archerPrefab;
        public GameObject arrowPrefab;
        public GameObject armyPrefabTeam1;
        public GameObject armyPrefabTeam2;
        public GameObject battleMapPrefab;
        public GameObject townPrefab;
        public GameObject markerPrefab;
        public GameObject townTeamMarkerTeam1Prefab;
        public GameObject townTeamMarkerTeam2Prefab;
        public GameObject millPrefab;
        public GameObject lumberjackHutPrefab;
        public GameObject stoneMinePrefab;
        public GameObject goldMinePrefab;
        public GameObject caravanPrefab;
        public GameObject battalionPrefab;

        public GameObject battalionShadowPrefab;

        //pre-battle menu
        public GameObject preBattleMarkerPrefab;

        //tiles
        public GameObject emptyTilePrefab;

        //red
        public GameObject redArcherTilePrefab;
        public GameObject redSwordsmanTilePrefab;

        public GameObject redCavalryTilePrefab;

        //blue
        public GameObject blueArcherTilePrefab;
        public GameObject blueSwordsmanTilePrefab;
        public GameObject blueCavalryTilePrefab;
    }

    public class PrefabHolderBaker : Baker<PrefabHolderAuthoring>
    {
        public override void Bake(PrefabHolderAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(entity, new PrefabHolder
            {
                soldierPrefab = GetEntity(authoring.soldierPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                archerPrefab = GetEntity(authoring.archerPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                arrowPrefab = GetEntity(authoring.arrowPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                armyPrefabTeam1 = GetEntity(authoring.armyPrefabTeam1,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                armyPrefabTeam2 = GetEntity(authoring.armyPrefabTeam2,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                battleMapPrefab = GetEntity(authoring.battleMapPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                townPrefab = GetEntity(authoring.townPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                markerPrefab = GetEntity(authoring.markerPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                townTeamMarkerTeam1Prefab = GetEntity(authoring.townTeamMarkerTeam1Prefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                townTeamMarkerTeam2Prefab = GetEntity(authoring.townTeamMarkerTeam2Prefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                millPrefab = GetEntity(authoring.millPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                lumberjackHutPrefab = GetEntity(authoring.lumberjackHutPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                stoneMinePrefab = GetEntity(authoring.stoneMinePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                goldMinePrefab = GetEntity(authoring.goldMinePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                caravanPrefab = GetEntity(authoring.caravanPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                battalionPrefab = GetEntity(authoring.battalionPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),

                preBattleMarkerPrefab = GetEntity(authoring.preBattleMarkerPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),

                battalionShadowPrefab = GetEntity(authoring.battalionShadowPrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),

                emptyTilePrefab = GetEntity(authoring.emptyTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                redArcherTilePrefab = GetEntity(authoring.redArcherTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                redSwordsmanTilePrefab = GetEntity(authoring.redSwordsmanTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                redCavalryTilePrefab = GetEntity(authoring.redCavalryTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                blueArcherTilePrefab = GetEntity(authoring.blueArcherTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                blueSwordsmanTilePrefab = GetEntity(authoring.blueSwordsmanTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic),
                blueCavalryTilePrefab = GetEntity(authoring.blueCavalryTilePrefab,
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic)
            });
        }
    }
}