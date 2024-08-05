using component._common.system_switchers;
using component.battle.battalion.data_holders;
using NUnit.Framework;
using system.battle.battalion.analysis.backup_plans;
using tests.testiky.utils;
using Unity.Collections;

namespace tests.testiky
{
    [TestFixture]
    public class CHM3_2_FindReinforcementPaths_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM3_2_FindReinforcementPaths>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void nooneNeedReinforcementsNoLinks()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.NO_VALID_PATH, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.NO_VALID_PATH, path2.pathType);
        }

        [Test]
        public void nooneNeedReinforcementsWithLink()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());

            chunkLinks.Add(0, 1);
            chunkLinks.Add(1, 0);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.NO_VALID_PATH, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.NO_VALID_PATH, path2.pathType);
        }

        [Test]
        public void needReinforcementsWithLink()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            needReinforcements.Add(0);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());

            chunkLinks.Add(0, 1);
            chunkLinks.Add(1, 0);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.TARGET, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.PATH, path2.pathType);
            Assert.AreEqual(1, path2.pathComplexity);
            Assert.AreEqual(0, path2.targetChunkId);
        }

        [Test]
        public void needReinforcementsWithoutLink()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            needReinforcements.Add(0);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.TARGET, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.NO_VALID_PATH, path2.pathType);
        }

        [Test]
        public void needReinforcementsWithLinkForMultiLine()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            needReinforcements.Add(0);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());
            allChunks.Add(2, new BattleChunk());

            chunkLinks.Add(0, 1);
            chunkLinks.Add(1, 0);
            chunkLinks.Add(2, 1);
            chunkLinks.Add(1, 2);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.TARGET, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.PATH, path2.pathType);
            Assert.AreEqual(1, path2.pathComplexity);
            Assert.AreEqual(0, path2.targetChunkId);
            var path3 = chunkReinforcementPaths[2];
            Assert.AreEqual(PathType.PATH, path3.pathType);
            Assert.AreEqual(2, path3.pathComplexity);
            Assert.AreEqual(1, path3.targetChunkId);
        }

        [Test]
        public void needReinforcementsWithLinkForMultiLinWithOverride()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();

            manager.AddComponentData(singletonEntity, dataHolder);

            var needReinforcements = new NativeHashSet<long>(10, Allocator.Temp);
            var chunkLinks = new NativeParallelMultiHashMap<long, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);

            needReinforcements.Add(0);
            needReinforcements.Add(3);

            allChunks.Add(0, new BattleChunk());
            allChunks.Add(1, new BattleChunk());
            allChunks.Add(2, new BattleChunk());
            allChunks.Add(3, new BattleChunk());

            chunkLinks.Add(0, 1);
            chunkLinks.Add(1, 0);
            chunkLinks.Add(2, 1);
            chunkLinks.Add(1, 2);
            chunkLinks.Add(2, 3);
            chunkLinks.Add(3, 2);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                chunksNeedingReinforcements = needReinforcements,
                chunkReinforcementPaths = new NativeHashMap<long, ChunkPath>(10, Allocator.Temp),
                chunkLinks = chunkLinks
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM3_2_FindReinforcementPaths>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var chunkReinforcementPaths = backupPlanDataHolder.chunkReinforcementPaths;

            var path1 = chunkReinforcementPaths[0];
            Assert.AreEqual(PathType.TARGET, path1.pathType);
            var path2 = chunkReinforcementPaths[1];
            Assert.AreEqual(PathType.PATH, path2.pathType);
            Assert.AreEqual(1, path2.pathComplexity);
            Assert.AreEqual(0, path2.targetChunkId);
            var path3 = chunkReinforcementPaths[2];
            Assert.AreEqual(PathType.PATH, path3.pathType);
            Assert.AreEqual(1, path3.pathComplexity);
            Assert.AreEqual(3, path3.targetChunkId);
            var path4 = chunkReinforcementPaths[3];
            Assert.AreEqual(PathType.TARGET, path4.pathType);
        }
    }
}