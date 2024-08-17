using System;
using system.battle.enums;
using Unity.Mathematics;

namespace system.battle.battalion.analysis.utils
{
    public class BattleTransformUtils
    {
        /**
         * Compares if 2 units are close enough to each other
         */
        public static bool isTooFar(float3 position1, float3 position2, float mySize, float otherSize, float safetyMargin = 1.1f)
        {
            var distance = math.abs(position1.x - position2.x);
            var sizeSum = (mySize + otherSize) / 2;
            // 1.1 = safety margin
            return distance > (sizeSum * safetyMargin);
        }

        /**
        * Compares if 2 units are close enough to each other for split (1 can create new unit in gap)
        */
        public static bool isTooFarForSplit(float3 position1, float3 position2, float mySize, float otherSize, float safetyMargin = 1.1f)
        {
            var distance = math.abs(position1.x - position2.x);
            var neededSpace = (mySize + otherSize) / 2 + mySize;
            return distance > (neededSpace * safetyMargin);
        }

        public static float3 getNewPositionForSplit(float3 myCurrentPosition, float width, Direction direction, float safetyMargin = 1.1f)
        {
            var xDelta = direction switch
            {
                Direction.LEFT => -width * safetyMargin,
                Direction.RIGHT => width * safetyMargin,
                _ => throw new Exception("Unsupported direction split " + direction)
            };
            var newX = myCurrentPosition.x + xDelta;
            return new float3(newX, myCurrentPosition.y, myCurrentPosition.z);
        }
    }
}