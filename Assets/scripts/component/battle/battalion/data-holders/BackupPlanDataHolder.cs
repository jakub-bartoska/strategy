namespace component.battle.battalion.data_holders
{
    public struct BackupPlanDataHolder : IComponentData
    {
        public NativeParallelMultiHashMap<TeamRow, BattleChunk> battleChunks;
        public NativeParallelMultiHashMap<TeamRow, BattleChunk> emptyChunks;
        public NativeList<BattalionInfo> moveLeft;
        public NativeList<BattalionInfo> moveRight;
        public NativeList<BattalionInfo> moveToDifferentChunk;
    }

    public struct TeamRow : IEquatable<TeamRow>
    {
        public int rowId;
        public Team team;

        public bool Equals(TeamRow other)
        {
            return rowId == other.rowId && (int) team == (int) other.team;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(rowId, (int) team);
        }
    }

    public struct BattleChunk
    {
        public int rowId;
        public bool leftFighting;
        public bool rightFighting;
        public NativeList<long> battalions;
        public float startX;
        public float endX;
        public Team team;
    }
}