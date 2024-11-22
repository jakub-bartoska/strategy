using System;
using component.battle.battalion;
using system.battle.enums;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.utils
{
    public class CustomTransformUtils
    {
        public static float3 defaulBattleMapOffset = new(10000, 0, 10000);
        public static float battleXSize = 300; //to each side

        public static LocalTransform getMapTransform()
        {
            return LocalTransform.FromPosition(defaulBattleMapOffset);
        }

        public static float3 getBattalionPositionForSoldiers(float x, float z)
        {
            var newX = (x - defaulBattleMapOffset.x) * 4;
            var newZ = (z - defaulBattleMapOffset.z) * 10;
            var distanceFromMiddle = -90;
            return new float3
            {
                x = newX * 5 + distanceFromMiddle + defaulBattleMapOffset.x,
                y = 0 + defaulBattleMapOffset.y,
                z = defaulBattleMapOffset.z + 40 - (newZ * 10)
            };
        }

        public static LocalTransform getBattalionPosition(float3 position)
        {
            var x = (position.x - defaulBattleMapOffset.x) * 4;
            var z = (position.z - defaulBattleMapOffset.z) * 10;
            var res = getBattalionPositionForSoldiers(x, z);
            return LocalTransform.FromPosition(res);
        }

        public static float getBattalionZPosition(int row)
        {
            return getBattalionPositionForSoldiers(0, row).z + 5f;
        }

        public static float3 calculateDesiredPosition(float3 originalPosition, BattalionWidth myWidth, BattalionWidth otherWidth, Direction direction, bool isExactPosition)
        {
            switch (direction)
            {
                case Direction.UP:
                case Direction.DOWN:
                    return calculateDesiredPosition(originalPosition, direction, isExactPosition);
                case Direction.LEFT:
                case Direction.RIGHT:
                    return calculateDesiredPositionInRow(originalPosition, myWidth, otherWidth, direction, isExactPosition);
                default:
                    throw new Exception("Unknown direction");
            }
        }

        private static float3 calculateDesiredPositionInRow(float3 originalPosition, BattalionWidth myWidth, BattalionWidth otherWidth, Direction direction, bool isExactPosition)
        {
            var diff = (myWidth.value + otherWidth.value) / 2 * 1.1f;
            var result = new float3
            {
                x = originalPosition.x + diff,
                y = originalPosition.y,
                z = originalPosition.z
            };

            switch (direction)
            {
                case Direction.LEFT:
                    result.x -= diff;
                    break;
                case Direction.RIGHT:
                    result.x += diff;
                    break;
                default:
                    throw new Exception("Invalid direction");
            }

            return result;
        }

        public static float3 calculateDesiredPosition(float3 originalPosition, Direction direction, bool isExactPosition)
        {
            var result = new float3
            {
                x = originalPosition.x,
                y = originalPosition.y,
                z = originalPosition.z
            };
            switch (direction)
            {
                case Direction.UP:
                    originalPosition.z += 10;
                    break;
                case Direction.DOWN:
                    originalPosition.z -= 10;
                    break;
                default:
                    throw new Exception("Invalid direction");
            }

            return result;
        }
    }
}