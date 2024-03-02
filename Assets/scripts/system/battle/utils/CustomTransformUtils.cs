using System;
using component.battle.battalion;
using system.battle.enums;
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

        public static float getBattalionZPosition(int row)
        {
            return getBattalionPositionForSoldiers(0, row).z + 5f;
        }

        public static float3 calculateDesiredPosition(float3 originalPosition, BattalionWidth myWidth, BattalionWidth otherWidth, Direction direction)
        {
            switch (direction)
            {
                case Direction.UP:
                case Direction.DOWN:
                    return calculateDesiredPosition(originalPosition, direction);
                case Direction.LEFT:
                case Direction.RIGHT:
                    return calculateDesiredPositionInRow(originalPosition, myWidth, otherWidth, direction);
                default:
                    throw new Exception("Unknown direction");
            }
        }

        private static float3 calculateDesiredPositionInRow(float3 originalPosition, BattalionWidth myWidth, BattalionWidth otherWidth, Direction direction)
        {
            var diff = (myWidth.value + otherWidth.value) / 2 * 1.1f;
            switch (direction)
            {
                case Direction.LEFT:
                    return new float3
                    {
                        x = originalPosition.x - diff,
                        y = originalPosition.y,
                        z = originalPosition.z
                    };
                case Direction.RIGHT:
                    return new float3
                    {
                        x = originalPosition.x + diff,
                        y = originalPosition.y,
                        z = originalPosition.z
                    };
                default:
                    throw new Exception("Invalid direction");
            }
        }

        public static float3 calculateDesiredPosition(float3 originalPostion, Direction direction)
        {
            switch (direction)
            {
                case Direction.UP:
                    return new float3
                    {
                        x = originalPostion.x,
                        y = originalPostion.y,
                        z = originalPostion.z + 10
                    };
                case Direction.DOWN:
                    return new float3
                    {
                        x = originalPostion.x,
                        y = originalPostion.y,
                        z = originalPostion.z - 10
                    };
                default:
                    throw new Exception("Invalid direction");
            }
        }
    }
}