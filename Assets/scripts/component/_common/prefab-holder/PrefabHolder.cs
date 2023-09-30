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
        public Entity caravanPrefab;
    }
}