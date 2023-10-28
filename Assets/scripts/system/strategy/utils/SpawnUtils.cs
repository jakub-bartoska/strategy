using component;
using component.authoring_pairs.PrefabHolder;
using component.strategy.general;
using Unity.Entities;
using Unity.Transforms;

namespace system.strategy.utils
{
    public class SpawnUtils
    {
        public static Entity spawnTeamMarker(EntityCommandBuffer ecb, TeamComponent team, Entity townEntity, PrefabHolder prefabHolder)
        {
            var prefab = team.team == Team.TEAM1
                ? prefabHolder.townTeamMarkerTeam1Prefab
                : prefabHolder.townTeamMarkerTeam2Prefab;
            var townTeamMarker = ecb.Instantiate(prefab);
            ecb.SetName(townTeamMarker, "Town team marker");
            ecb.AddComponent(townTeamMarker, new Parent
            {
                Value = townEntity
            });
            return townTeamMarker;
        }
    }
}