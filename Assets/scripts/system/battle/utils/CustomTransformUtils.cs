using component;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class CustomTransformUtils
    {
        private static float3 defaulBattleMapOffset = new(10000, 0, 10000);

        public static LocalTransform getMapTransform()
        {
            return LocalTransform.FromPosition(defaulBattleMapOffset);
        }

        public static float3 getBattalionPositionForSoldiers(Team team, int x, int y)
        {
            var distanceFromMiddle = team switch
            {
                Team.TEAM1 => 50,
                Team.TEAM2 => -50,
            };
            return new float3
            {
                x = x * 5 + distanceFromMiddle + defaulBattleMapOffset.x,
                y = 0 + defaulBattleMapOffset.y,
                z = defaulBattleMapOffset.z + 40 - (y * 10)
            };
        }

        public static LocalTransform getBattalionPosition(Team team, int x, int y)
        {
            var position = getBattalionPositionForSoldiers(team, x, y);
            position.y = 0.02f;
            position.z += 5f;
            return LocalTransform.FromPosition(position);
        }

        public static float getBattalionZPosition(int z)
        {
            return getBattalionPositionForSoldiers(Team.TEAM1, 0, z).z + 5f;
        }
    }
}