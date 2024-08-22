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
    public class CHM2_AddEmptyChunks_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM2_0_AddEmptyChunks>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void addEmptyChunk()
        {
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            };
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder(2);
            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                lastChunkId = 0
            };
            var chunk = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                leftEnemy = null,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM1
            };
            backupPlanHolder.allChunks.Add(chunk.chunkId, chunk);
            backupPlanHolder.battleChunksPerRowTeam.Add(team1Key, chunk.chunkId);
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_0_AddEmptyChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            });
            iterator.MoveNext();
            var emptyChunk = allChunks[iterator.Current];
            //Assert.AreEqual(0, emptyChunk.battalions.Length);
            Assert.AreEqual(false, emptyChunk.leftEnemy);
            Assert.AreEqual(false, emptyChunk.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk.team);
            Assert.AreEqual(1, emptyChunk.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk.startX);
            Assert.AreEqual(10000 + 300, emptyChunk.endX);
        }

        [Test]
        public void addEmptyChunkWhenThereIsEnemyChunk()
        {
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 1
            };
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder(2);
            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                lastChunkId = 0
            };
            var chunk1 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                leftEnemy = null,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM1
            };
            backupPlanHolder.allChunks.Add(chunk1.chunkId, chunk1);
            backupPlanHolder.battleChunksPerRowTeam.Add(team1Key, chunk1.chunkId);
            var chunk2 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 1,
                leftEnemy = null,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM2
            };
            backupPlanHolder.allChunks.Add(chunk2.chunkId, chunk2);
            backupPlanHolder.battleChunksPerRowTeam.Add(team2Key, chunk2.chunkId);
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_0_AddEmptyChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            });
            iterator.MoveNext();
            var emptyChunk2 = allChunks[iterator.Current];
            iterator.MoveNext();
            var emptyChunk1 = allChunks[iterator.Current];
            Assert.AreEqual(false, emptyChunk1.leftEnemy);
            Assert.AreEqual(true, emptyChunk1.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(1, emptyChunk1.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk1.startX);
            Assert.AreEqual(10, emptyChunk1.endX);

            Assert.AreEqual(true, emptyChunk2.leftEnemy);
            Assert.AreEqual(false, emptyChunk2.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk2.team);
            Assert.AreEqual(1, emptyChunk2.rowId);
            Assert.AreEqual(100, emptyChunk2.startX);
            Assert.AreEqual(10000 + 300, emptyChunk2.endX);
        }

        [Test]
        public void sameRowLeftOnly()
        {
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 0
            };
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder(2);
            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                lastChunkId = 0
            };
            var chunk1 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                //leftEnemy = true,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            };
            backupPlanHolder.allChunks.Add(chunk1.chunkId, chunk1);
            backupPlanHolder.battleChunksPerRowTeam.Add(team1Key, chunk1.chunkId);
            var chunk2 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                leftEnemy = null,
                //rightEnemy = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = -10,
                endX = 10,
                team = Team.TEAM2
            };
            backupPlanHolder.allChunks.Add(chunk2.chunkId, chunk2);
            backupPlanHolder.battleChunksPerRowTeam.Add(team2Key, chunk2.chunkId);
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_0_AddEmptyChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk1 = allChunks[iterator.Current];
            Assert.AreEqual(false, emptyChunk1.leftEnemy);
            Assert.AreEqual(true, emptyChunk1.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(0, emptyChunk1.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk1.startX);
            Assert.AreEqual(-10, emptyChunk1.endX);
        }

        [Test]
        public void sameRowRightOnly()
        {
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 0
            };
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder(2);
            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                lastChunkId = 0
            };
            var chunk1 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                leftEnemy = null,
                //rightEnemy = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            };
            backupPlanHolder.allChunks.Add(chunk1.chunkId, chunk1);
            backupPlanHolder.battleChunksPerRowTeam.Add(team1Key, chunk1.chunkId);
            var chunk2 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                //leftEnemy = true,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 30,
                endX = 100,
                team = Team.TEAM2
            };
            backupPlanHolder.allChunks.Add(chunk2.chunkId, chunk2);
            backupPlanHolder.battleChunksPerRowTeam.Add(team2Key, chunk2.chunkId);
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_0_AddEmptyChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk1 = allChunks[iterator.Current];
            Assert.AreEqual(true, emptyChunk1.leftEnemy);
            Assert.AreEqual(false, emptyChunk1.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(0, emptyChunk1.rowId);
            Assert.AreEqual(100, emptyChunk1.startX);
            Assert.AreEqual(10000 + 300, emptyChunk1.endX);
        }

        [Test]
        public void sameRowBothSides()
        {
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 0
            };
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder(2);
            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp),
                lastChunkId = 0
            };
            var chunk1 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                //leftEnemy = true,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            };
            backupPlanHolder.allChunks.Add(chunk1.chunkId, chunk1);
            backupPlanHolder.battleChunksPerRowTeam.Add(team1Key, chunk1.chunkId);
            var chunk2 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                //leftEnemy = true,
                rightEnemy = null,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 30,
                endX = 100,
                team = Team.TEAM2
            };
            backupPlanHolder.allChunks.Add(chunk2.chunkId, chunk2);
            backupPlanHolder.battleChunksPerRowTeam.Add(team2Key, chunk2.chunkId);
            var chunk3 = new BattleChunk
            {
                chunkId = backupPlanHolder.lastChunkId++,
                rowId = 0,
                leftEnemy = null,
                //rightEnemy = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = -10,
                endX = 10,
                team = Team.TEAM2
            };
            backupPlanHolder.allChunks.Add(chunk3.chunkId, chunk3);
            backupPlanHolder.battleChunksPerRowTeam.Add(team2Key, chunk3.chunkId);
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_0_AddEmptyChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk2 = allChunks[iterator.Current];
            iterator.MoveNext();
            var emptyChunk1 = allChunks[iterator.Current];
            Assert.AreEqual(false, emptyChunk1.leftEnemy);
            Assert.AreEqual(true, emptyChunk1.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(0, emptyChunk1.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk1.startX);
            Assert.AreEqual(-10, emptyChunk1.endX);

            Assert.AreEqual(true, emptyChunk2.leftEnemy);
            Assert.AreEqual(false, emptyChunk2.rightEnemy);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk2.team);
            Assert.AreEqual(0, emptyChunk2.rowId);
            Assert.AreEqual(100, emptyChunk2.startX);
            Assert.AreEqual(10000 + 300, emptyChunk2.endX);
        }
    }
}