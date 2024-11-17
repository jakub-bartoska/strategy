using Unity.Entities;

namespace component.authoring_pairs.PrefabHolder
{
    public struct PrefabHolder : IComponentData
    {
        public Entity soldierPrefab;
        public Entity archerPrefab;
        public Entity arrowPrefab;
        public Entity armyPrefabTeam1;
        public Entity armyPrefabTeam2;
        public Entity battleMapPrefab;
        public Entity townPrefab;
        public Entity markerPrefab;
        public Entity townTeamMarkerTeam1Prefab;
        public Entity townTeamMarkerTeam2Prefab;
        public Entity millPrefab;
        public Entity lumberjackHutPrefab;
        public Entity stoneMinePrefab;
        public Entity goldMinePrefab;
        public Entity caravanPrefab;
        public Entity battalionPrefab;

        public Entity battalionShadowPrefab;

        //pre-battle menu
        public Entity preBattleMarkerPrefab;

        //tiles
        public Entity emptyTilePrefab;

        //red
        public Entity redArcherTilePrefab;
        public Entity redSwordsmanTilePrefab;
        public Entity redCavalryTilePrefab;

        //blue
        public Entity blueArcherTilePrefab;
        public Entity blueSwordsmanTilePrefab;
        public Entity blueCavalryTilePrefab;
    }
}