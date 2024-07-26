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