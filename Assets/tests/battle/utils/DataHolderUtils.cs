namespace tests.testiky.utils
{
    public class DataHolderUtils
    {
        public static DataHolder createBasicDataholder(int rows = 10)
        {
            var allRowIds = new NativeList<int>(Allocator.Temp);
            for (int i = 0; i < rows; i++)
            {
                allRowIds.Add(i);
            }

            var dataHolder = new DataHolder
            {
                allRowIds = allRowIds
            };
            return dataHolder;
        }

        public static BattalionInfo createBattalion(float3 position, Team team, long id = -1)
        {
            var size = BattalionSpawner.getSizeForBattalionType(SoldierType.SWORDSMAN);
            return new BattalionInfo
            {
                position = position,
                team = team,
                width = size,
                battalionId = id,
                unitType = BattleUnitTypeEnum.BATTALION
            };
        }

        public static NativeParallelMultiHashMap<int, BattalionInfo> createPositions(BattalionInfo[] battalions)
        {
            var result = new NativeParallelMultiHashMap<int, BattalionInfo>(battalions.Length, Allocator.Temp);
            foreach (var soldier in battalions)
            {
                result.Add(1, soldier);
            }

            return result;
        }

        public static BattleChunk getChunkByTeamPosition(Entity singletonEntity, Team team, EntityManager manager, int position = 0, int row = 1)
        {
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = team,
                rowId = row
            });
            for (int i = 0; i <= position; i++)
            {
                iterator.MoveNext();
            }

            return iterator.Current;
        }
    }
}