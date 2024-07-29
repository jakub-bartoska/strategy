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
            CreateSystem<CHM2_AddEmptyChunks>();
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
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };
            backupPlanHolder.battleChunks.Add(team1Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = false,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM1
            });
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_AddEmptyChunks>();

            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            });
            iterator.MoveNext();
            var emptyChunk = iterator.Current;
            //Assert.AreEqual(0, emptyChunk.battalions.Length);
            Assert.AreEqual(false, emptyChunk.leftFighting);
            Assert.AreEqual(false, emptyChunk.rightFighting);
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
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };
            backupPlanHolder.battleChunks.Add(team1Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = false,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM1
            });
            backupPlanHolder.battleChunks.Add(team2Key, new BattleChunk
            {
                rowId = 1,
                leftFighting = false,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 100,
                team = Team.TEAM2
            });
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_AddEmptyChunks>();

            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            });
            iterator.MoveNext();
            var emptyChunk2 = iterator.Current;
            iterator.MoveNext();
            var emptyChunk1 = iterator.Current;
            Assert.AreEqual(false, emptyChunk1.leftFighting);
            Assert.AreEqual(true, emptyChunk1.rightFighting);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(1, emptyChunk1.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk1.startX);
            Assert.AreEqual(10, emptyChunk1.endX);

            Assert.AreEqual(true, emptyChunk2.leftFighting);
            Assert.AreEqual(false, emptyChunk2.rightFighting);
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
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };
            backupPlanHolder.battleChunks.Add(team1Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = true,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            });
            backupPlanHolder.battleChunks.Add(team2Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = false,
                rightFighting = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = -10,
                endX = 10,
                team = Team.TEAM2
            });
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_AddEmptyChunks>();

            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk1 = iterator.Current;
            Assert.AreEqual(false, emptyChunk1.leftFighting);
            Assert.AreEqual(true, emptyChunk1.rightFighting);
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
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };
            backupPlanHolder.battleChunks.Add(team1Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = false,
                rightFighting = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            });
            backupPlanHolder.battleChunks.Add(team2Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = true,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 30,
                endX = 100,
                team = Team.TEAM2
            });
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_AddEmptyChunks>();

            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk1 = iterator.Current;
            Assert.AreEqual(true, emptyChunk1.leftFighting);
            Assert.AreEqual(false, emptyChunk1.rightFighting);
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
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp),
                emptyChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };
            backupPlanHolder.battleChunks.Add(team1Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = true,
                rightFighting = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 10,
                endX = 30,
                team = Team.TEAM1
            });
            backupPlanHolder.battleChunks.Add(team2Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = true,
                rightFighting = false,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = 30,
                endX = 100,
                team = Team.TEAM2
            });
            backupPlanHolder.battleChunks.Add(team2Key, new BattleChunk
            {
                rowId = 0,
                leftFighting = false,
                rightFighting = true,
                battalions = new NativeList<long>(0, Allocator.Persistent),
                startX = -10,
                endX = 10,
                team = Team.TEAM2
            });
            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM2_AddEmptyChunks>();

            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).emptyChunks;
            var iterator = battleChunks.GetValuesForKey(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 0
            });
            iterator.MoveNext();
            var emptyChunk2 = iterator.Current;
            iterator.MoveNext();
            var emptyChunk1 = iterator.Current;
            Assert.AreEqual(false, emptyChunk1.leftFighting);
            Assert.AreEqual(true, emptyChunk1.rightFighting);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk1.team);
            Assert.AreEqual(0, emptyChunk1.rowId);
            Assert.AreEqual(10000 - 300, emptyChunk1.startX);
            Assert.AreEqual(-10, emptyChunk1.endX);

            Assert.AreEqual(true, emptyChunk2.leftFighting);
            Assert.AreEqual(false, emptyChunk2.rightFighting);
            Assert.AreEqual((int) Team.TEAM1, (int) emptyChunk2.team);
            Assert.AreEqual(0, emptyChunk2.rowId);
            Assert.AreEqual(100, emptyChunk2.startX);
            Assert.AreEqual(10000 + 300, emptyChunk2.endX);
        }
    }
}