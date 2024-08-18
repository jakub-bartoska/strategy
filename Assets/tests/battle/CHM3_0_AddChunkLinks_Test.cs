using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using NUnit.Framework;
using system.battle.battalion.analysis.backup_plans;
using tests.testiky.utils;
using Unity.Collections;

namespace tests.testiky
{
    [TestFixture]
    public class CHM3_0_AddChunkLinks_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM3_3_AddChunkLinks>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void chunks_dont_cross_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                startX = 0,
                endX = 1,
                team = Team.TEAM1
            };
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk.team,
                rowId = chunk.rowId
            }, chunk.chunkId);

            var chunk2 = new BattleChunk
            {
                chunkId = 1,
                rowId = 2,
                startX = 2,
                endX = 3,
                team = Team.TEAM1
            };
            allChunks.Add(chunk2.chunkId, chunk2);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk2.team,
                rowId = chunk2.rowId
            }, chunk2.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(0, Allocator.Temp),
                chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_3_AddChunkLinks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunkLinks;

            Assert.AreEqual(0, moveToDifferentChunk.CountValuesForKey(0));
            Assert.AreEqual(0, moveToDifferentChunk.CountValuesForKey(1));
        }

        [Test]
        public void cross_from_left_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                startX = 0,
                endX = 2,
                team = Team.TEAM1
            };
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk.team,
                rowId = chunk.rowId
            }, chunk.chunkId);

            var chunk2 = new BattleChunk
            {
                chunkId = 1,
                rowId = 2,
                startX = 1,
                endX = 3,
                team = Team.TEAM1
            };
            allChunks.Add(chunk2.chunkId, chunk2);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk2.team,
                rowId = chunk2.rowId
            }, chunk2.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(0, Allocator.Temp),
                chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_3_AddChunkLinks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunkLinks;

            var iterator = moveToDifferentChunk.GetValuesForKey(0);
            iterator.MoveNext();
            var link1 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(0));
            Assert.AreEqual(1, link1);
            iterator = moveToDifferentChunk.GetValuesForKey(1);
            iterator.MoveNext();
            var link2 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(1));
            Assert.AreEqual(0, link2);
        }

        [Test]
        public void cross_full_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                startX = 0,
                endX = 3,
                team = Team.TEAM1
            };
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk.team,
                rowId = chunk.rowId
            }, chunk.chunkId);

            var chunk2 = new BattleChunk
            {
                chunkId = 1,
                rowId = 2,
                startX = 1,
                endX = 2,
                team = Team.TEAM1
            };
            allChunks.Add(chunk2.chunkId, chunk2);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk2.team,
                rowId = chunk2.rowId
            }, chunk2.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(0, Allocator.Temp),
                chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_3_AddChunkLinks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunkLinks;

            var iterator = moveToDifferentChunk.GetValuesForKey(0);
            iterator.MoveNext();
            var link1 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(0));
            Assert.AreEqual(1, link1);
            iterator = moveToDifferentChunk.GetValuesForKey(1);
            iterator.MoveNext();
            var link2 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(1));
            Assert.AreEqual(0, link2);
        }

        [Test]
        public void cross_from_right_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                startX = 0,
                endX = 2,
                team = Team.TEAM1
            };
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk.team,
                rowId = chunk.rowId
            }, chunk.chunkId);

            var chunk2 = new BattleChunk
            {
                chunkId = 1,
                rowId = 2,
                startX = -1,
                endX = 1,
                team = Team.TEAM1
            };
            allChunks.Add(chunk2.chunkId, chunk2);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk2.team,
                rowId = chunk2.rowId
            }, chunk2.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(0, Allocator.Temp),
                chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_3_AddChunkLinks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunkLinks;

            var iterator = moveToDifferentChunk.GetValuesForKey(0);
            iterator.MoveNext();
            var link1 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(0));
            Assert.AreEqual(1, link1);
            iterator = moveToDifferentChunk.GetValuesForKey(1);
            iterator.MoveNext();
            var link2 = iterator.Current;
            Assert.AreEqual(1, moveToDifferentChunk.CountValuesForKey(1));
            Assert.AreEqual(0, link2);
        }

        [Test]
        public void chunks_dont_cross_right_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                startX = 0,
                endX = 1,
                team = Team.TEAM1
            };
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk.team,
                rowId = chunk.rowId
            }, chunk.chunkId);

            var chunk2 = new BattleChunk
            {
                chunkId = 1,
                rowId = 2,
                startX = -2,
                endX = -1,
                team = Team.TEAM1
            };
            allChunks.Add(chunk2.chunkId, chunk2);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = chunk2.team,
                rowId = chunk2.rowId
            }, chunk2.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(0, Allocator.Temp),
                chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_3_AddChunkLinks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunkLinks;

            Assert.AreEqual(0, moveToDifferentChunk.CountValuesForKey(0));
            Assert.AreEqual(0, moveToDifferentChunk.CountValuesForKey(1));
        }
    }
}