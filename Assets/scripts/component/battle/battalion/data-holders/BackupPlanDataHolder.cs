using System;
using Unity.Collections;
using Unity.Entities;

namespace component.battle.battalion.data_holders
{
    public struct BackupPlanDataHolder : IComponentData
    {
        public NativeHashMap<long, BattleChunk> allChunks;
        public NativeParallelMultiHashMap<TeamRow, long> battleChunksPerRowTeam;
        public NativeParallelMultiHashMap<TeamRow, long> emptyChunks;

        /**
         * list of battalion ids, which should move left within their current chunk
         */
        public NativeList<long> moveLeft;

        /**
         * list of battalion ids, which should move right within their current chunk
         */
        public NativeList<long> moveRight;

        /**
         * list of battalions IDs, which should move out of their current chunk
         */
        public NativeList<long> moveToDifferentChunk;

        public long lastChunkId;

        /**
         * chunkId -> list of neighbouring chunkIds
         *
         * can contain dublicities A->B, B->A
         */
        public NativeParallelMultiHashMap<long, long> chunkLinks;

        /**
         * chunkId
         *
         * If chunk is fighting from 1 or both sides and dont have battalion to fight enemy facing that dirrection, then chunk is marked as it needs additional reinforcements
         */
        public NativeHashSet<long> chunksNeedingReinforcements;

        /**
         * chunkId -> path to chung which needs reinforcement
         */
        public NativeHashMap<long, ChunkPath> chunkReinforcementPaths;

        /**
         * Battalionid - chunk id
         *
         * holds info which battalion belongs to which chunk
         */
        public NativeHashMap<long, long> battalionIdToChunk;
    }

    public struct ChunkPath
    {
        /**
         * How many chunks have to be passed till battalion gets to target chunk
         */
        public int pathComplexity;

        /**
         * Next chunk id in the path
         */
        public long targetChunkId;

        public PathType pathType;
    }

    public enum PathType
    {
        /**
         * chunk needs reinforcements
         */
        TARGET,

        /**
         * chunk is linked with target chunk (linked by 0-n long chain)
         */
        PATH,

        /**
         * chunk is not linked with target chunk
         */
        NO_VALID_PATH
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
        public long chunkId;
        public int rowId;
        public bool leftFighting;
        public bool rightFighting;
        public NativeList<long> battalions;
        public float startX;
        public float endX;
        public Team team;
    }
}