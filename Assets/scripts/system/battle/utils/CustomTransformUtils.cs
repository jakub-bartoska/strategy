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

        public static float3 getBattalionPositionForSoldiers(int x, int y)
        {
            var distanceFromMiddle = -90;
            return new float3
            {
                x = x * 5 + distanceFromMiddle + defaulBattleMapOffset.x,
                y = 0 + defaulBattleMapOffset.y,
                z = defaulBattleMapOffset.z + 40 - (y * 10)
            };
        }

        public static LocalTransform getBattalionPosition(int x, int y)
        {
            var position = getBattalionPositionForSoldiers(x, y);
            position.y = 0.02f;
            position.z += 5f;
            return LocalTransform.FromPosition(position);
        }

        public static float getBattalionZPosition(int z)
        {
            return getBattalionPositionForSoldiers(0, z).z + 5f;
        }
    }
}