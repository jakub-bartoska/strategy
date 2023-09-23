using Unity.Entities;
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
                    TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic)
            });
        }
    }
}