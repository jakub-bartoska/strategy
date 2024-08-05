using component._common.system_switchers;
using component.battle.battalion.data_holders;
using NUnit.Framework;
using system.battle.battalion.analysis.backup_plans;
using tests.testiky.utils;
using Unity.Collections;

namespace tests.testiky
{
    [TestFixture]
    public class CHM3_1_FindNeedReinforcementChunks_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM3_1_FindNeedReinforcementChunks>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void noone_need_reinforcement_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = false,
                rightFighting = false,
                battalions = new NativeList<long>(10, Allocator.Temp)
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(0, moveToDifferentChunk.Count);
        }

        [Test]
        public void left_reinforcement_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = true,
                rightFighting = false,
                battalions = new NativeList<long>(10, Allocator.Temp)
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(1, moveToDifferentChunk.Count);
            Assert.AreEqual(true, moveToDifferentChunk.Contains(1));
        }

        [Test]
        public void right_reinforcement_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = false,
                rightFighting = true,
                battalions = new NativeList<long>(10, Allocator.Temp)
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(1, moveToDifferentChunk.Count);
            Assert.AreEqual(true, moveToDifferentChunk.Contains(1));
        }

        [Test]
        public void both_sides_reinforcement_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = true,
                rightFighting = true,
                battalions = new NativeList<long>(10, Allocator.Temp)
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(1, moveToDifferentChunk.Count);
            Assert.AreEqual(true, moveToDifferentChunk.Contains(1));
        }

        [Test]
        public void both_sides_reinforcement_with_one_battalion_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = true,
                rightFighting = true,
                battalions = battalions
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(1, moveToDifferentChunk.Count);
            Assert.AreEqual(true, moveToDifferentChunk.Contains(1));
        }

        [Test]
        public void both_sides_reinforcement_with_two_battalion_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);
            battalions.Add(2);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = true,
                rightFighting = true,
                battalions = battalions
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(0, moveToDifferentChunk.Count);
        }

        [Test]
        public void left_full_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = true,
                rightFighting = false,
                battalions = battalions
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(0, moveToDifferentChunk.Count);
        }

        [Test]
        public void right_full_test()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);

            var chunk = new BattleChunk
            {
                chunkId = 1,
                leftFighting = false,
                rightFighting = true,
                battalions = battalions
            };
            allChunks.Add(chunk.chunkId, chunk);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = new NativeHashSet<long>(10, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_1_FindNeedReinforcementChunks>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveToDifferentChunk = backupPlanDataHolder.chunksNeedingReinforcements;

            Assert.AreEqual(0, moveToDifferentChunk.Count);
        }
    }
}