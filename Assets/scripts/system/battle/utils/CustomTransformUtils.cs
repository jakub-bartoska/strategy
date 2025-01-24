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

        public static LocalTransform getBattalionPosition(float3 position)
        {
            return LocalTransform.FromPosition(position);
        }

        public static float getBattalionZPosition(int row, int maxRows)
        {
            return defaulBattleMapOffset.z + (maxRows / 2 - row) * 10 + 5;
        }

        public static float3 adjustPositionFromPreBattleToBattle(float3 oldPosition)
        {
            var positionDelta = oldPosition - defaulBattleMapOffset;
            var adjustedPosition = positionDelta * 10;
            adjustedPosition.z += 5;
            var result = adjustedPosition + defaulBattleMapOffset;
            result.y = 0.02f;
            return result;
        }

        public static float3 adjustPositionFromBattleToPreBattle(float3 oldPosition)
        {
            var positionDelta = oldPosition - defaulBattleMapOffset;
            positionDelta.z -= 5;
            var adjustedPosition = positionDelta / 10;
            var result = adjustedPosition + defaulBattleMapOffset;
            result.y = 0.02f;
            return result;
        }

        public static int positionToRow(float3 position, int maxRows)
        {
            return (int)((defaulBattleMapOffset.z - position.z - 5) / 10 + maxRows / 2);
        }

        public static float3 calculateDesiredPosition(float3 originalPosition, BattalionWidth myWidth,
            BattalionWidth otherWidth, Direction direction, bool isExactPosition)
        {
            switch (direction)
            {
                case Direction.UP:
                case Direction.DOWN:
                    return calculateDesiredPosition(originalPosition, direction, isExactPosition);
                case Direction.LEFT:
                case Direction.RIGHT:
                    return calculateDesiredPositionInRow(originalPosition, myWidth, otherWidth, direction,
                        isExactPosition);
                default:
                    throw new Exception("Unknown direction");
            }
        }

        private static float3 calculateDesiredPositionInRow(float3 originalPosition, BattalionWidth myWidth,
            BattalionWidth otherWidth, Direction direction, bool isExactPosition)
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

        public static float3 calculateDesiredPosition(float3 originalPosition, Direction direction,
            bool isExactPosition)
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