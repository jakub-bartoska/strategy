using Unity.Mathematics;

namespace system.battle.battalion.analysis.utils
{
    public class BattleTransformUtils
    {
        /**
         * Compares if 2 units are close enaugh to each other
         */
        public static bool isTooFar(float3 position1, float3 position2, float mySize, float otherSize, float safetyMargin = 1.1f)
        {
            var distance = math.abs(position1.x - position2.x);
            var sizeSum = (mySize + otherSize) / 2;
            // 1.1 = safety margin
            return distance > (sizeSum * safetyMargin);
        }
    }
}