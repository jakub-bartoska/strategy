using component;
using component._common.system_switchers;
using component.battle.battalion.data_holders;
using NUnit.Framework;
using system.battle.battalion.analysis.backup_plans;
using tests.testiky.utils;
using Unity.Collections;
using Unity.Mathematics;

namespace tests.testiky
{
    [TestFixture]
    public class CHM4_BasicChunkMovement_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM3_5_BasicChunkMovement>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void basic_test()
        {
            var singletonEntity = CreateEntity();

            var battalionInfo = new NativeHashMap<long, BattalionInfo>(5, Allocator.Temp);
            battalionInfo.Add(1, DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1));
            battalionInfo.Add(2, DataHolderUtils.createBattalion(new float3(1, 0, 0), Team.TEAM1, 2));
            battalionInfo.Add(3, DataHolderUtils.createBattalion(new float3(2, 0, 0), Team.TEAM1, 3));

            var dataHolder = DataHolderUtils.createBasicDataholder();
            dataHolder.battalionInfo = battalionInfo;

            manager.AddComponentData(singletonEntity, dataHolder);

            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);
            battalions.Add(2);
            battalions.Add(3);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                //leftEnemy = true,
                //rightEnemy = true,
                battalions = battalions,
                team = Team.TEAM1
            };
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            }, chunk.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                moveLeft = new NativeList<long>(100, Allocator.TempJob),
                moveRight = new NativeList<long>(100, Allocator.TempJob),
                moveToDifferentChunk = new NativeList<long>(100, Allocator.TempJob)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM3_5_BasicChunkMovement>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveLeft = backupPlanDataHolder.moveLeft;
            var moveRight = backupPlanDataHolder.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.moveToDifferentChunk;
            Assert.AreEqual(1, moveLeft.Length);
            Assert.AreEqual(1, moveRight.Length);
            Assert.AreEqual(1, moveToDifferentChunk.Length);
            Assert.AreEqual(1, moveLeft[0]);
            Assert.AreEqual(3, moveRight[0]);
            Assert.AreEqual(2, moveToDifferentChunk[0]);
        }

        [Test]
        public void correct_sort()
        {
            var singletonEntity = CreateEntity();

            var battalionInfo = new NativeHashMap<long, BattalionInfo>(5, Allocator.Temp);
            battalionInfo.Add(1, DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1));
            battalionInfo.Add(2, DataHolderUtils.createBattalion(new float3(1, 0, 0), Team.TEAM1, 2));
            battalionInfo.Add(3, DataHolderUtils.createBattalion(new float3(2, 0, 0), Team.TEAM1, 3));

            var dataHolder = DataHolderUtils.createBasicDataholder();
            dataHolder.battalionInfo = battalionInfo;

            manager.AddComponentData(singletonEntity, dataHolder);

            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(2);
            battalions.Add(3);
            battalions.Add(1);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                //leftEnemy = true,
                //rightEnemy = true,
                battalions = battalions,
                team = Team.TEAM1
            };

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            }, chunk.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                moveLeft = new NativeList<long>(100, Allocator.TempJob),
                moveRight = new NativeList<long>(100, Allocator.TempJob),
                moveToDifferentChunk = new NativeList<long>(100, Allocator.TempJob)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM3_5_BasicChunkMovement>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveLeft = backupPlanDataHolder.moveLeft;
            var moveRight = backupPlanDataHolder.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.moveToDifferentChunk;
            Assert.AreEqual(1, moveLeft.Length);
            Assert.AreEqual(1, moveRight.Length);
            Assert.AreEqual(1, moveToDifferentChunk.Length);
            Assert.AreEqual(1, moveLeft[0]);
            Assert.AreEqual(3, moveRight[0]);
            Assert.AreEqual(2, moveToDifferentChunk[0]);
        }

        [Test]
        public void left_only()
        {
            var singletonEntity = CreateEntity();

            var battalionInfo = new NativeHashMap<long, BattalionInfo>(5, Allocator.Temp);
            battalionInfo.Add(1, DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1));
            battalionInfo.Add(2, DataHolderUtils.createBattalion(new float3(1, 0, 0), Team.TEAM1, 2));
            battalionInfo.Add(3, DataHolderUtils.createBattalion(new float3(2, 0, 0), Team.TEAM1, 3));

            var dataHolder = DataHolderUtils.createBasicDataholder();
            dataHolder.battalionInfo = battalionInfo;

            manager.AddComponentData(singletonEntity, dataHolder);

            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);
            battalions.Add(2);
            battalions.Add(3);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                //leftEnemy = true,
                rightEnemy = null,
                battalions = battalions,
                team = Team.TEAM1
            };

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            }, chunk.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                moveLeft = new NativeList<long>(100, Allocator.TempJob),
                moveRight = new NativeList<long>(100, Allocator.TempJob),
                moveToDifferentChunk = new NativeList<long>(100, Allocator.TempJob)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM3_5_BasicChunkMovement>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveLeft = backupPlanDataHolder.moveLeft;
            var moveRight = backupPlanDataHolder.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.moveToDifferentChunk;
            Assert.AreEqual(1, moveLeft.Length);
            Assert.AreEqual(0, moveRight.Length);
            Assert.AreEqual(2, moveToDifferentChunk.Length);
            Assert.AreEqual(1, moveLeft[0]);
        }

        [Test]
        public void right_only()
        {
            var singletonEntity = CreateEntity();

            var battalionInfo = new NativeHashMap<long, BattalionInfo>(5, Allocator.Temp);
            battalionInfo.Add(1, DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1));
            battalionInfo.Add(2, DataHolderUtils.createBattalion(new float3(1, 0, 0), Team.TEAM1, 2));
            battalionInfo.Add(3, DataHolderUtils.createBattalion(new float3(2, 0, 0), Team.TEAM1, 3));

            var dataHolder = DataHolderUtils.createBasicDataholder();
            dataHolder.battalionInfo = battalionInfo;

            manager.AddComponentData(singletonEntity, dataHolder);

            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);
            battalions.Add(2);
            battalions.Add(3);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                leftEnemy = null,
                //rightEnemy = true,
                battalions = battalions,
                team = Team.TEAM1
            };

            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            }, chunk.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                moveLeft = new NativeList<long>(100, Allocator.TempJob),
                moveRight = new NativeList<long>(100, Allocator.TempJob),
                moveToDifferentChunk = new NativeList<long>(100, Allocator.TempJob)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM3_5_BasicChunkMovement>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveLeft = backupPlanDataHolder.moveLeft;
            var moveRight = backupPlanDataHolder.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.moveToDifferentChunk;
            Assert.AreEqual(0, moveLeft.Length);
            Assert.AreEqual(1, moveRight.Length);
            Assert.AreEqual(2, moveToDifferentChunk.Length);
            Assert.AreEqual(3, moveRight[0]);
        }

        [Test]
        public void lot_of_battalions()
        {
            var singletonEntity = CreateEntity();

            var battalionInfo = new NativeHashMap<long, BattalionInfo>(5, Allocator.Temp);
            battalionInfo.Add(1, DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1));
            battalionInfo.Add(2, DataHolderUtils.createBattalion(new float3(1, 0, 0), Team.TEAM1, 2));
            battalionInfo.Add(3, DataHolderUtils.createBattalion(new float3(2, 0, 0), Team.TEAM1, 3));
            battalionInfo.Add(4, DataHolderUtils.createBattalion(new float3(3, 0, 0), Team.TEAM1, 4));
            battalionInfo.Add(5, DataHolderUtils.createBattalion(new float3(4, 0, 0), Team.TEAM1, 5));
            battalionInfo.Add(6, DataHolderUtils.createBattalion(new float3(5, 0, 0), Team.TEAM1, 6));

            var dataHolder = DataHolderUtils.createBasicDataholder();
            dataHolder.battalionInfo = battalionInfo;

            manager.AddComponentData(singletonEntity, dataHolder);

            var battalions = new NativeList<long>(10, Allocator.Temp);
            battalions.Add(1);
            battalions.Add(2);
            battalions.Add(3);
            battalions.Add(4);
            battalions.Add(5);
            battalions.Add(6);

            var chunk = new BattleChunk
            {
                chunkId = 0,
                rowId = 1,
                //leftEnemy = true,
                //rightEnemy = true,
                battalions = battalions,
                team = Team.TEAM1
            };

            var battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(10, Allocator.Temp);
            var allChunks = new NativeHashMap<long, BattleChunk>(10, Allocator.Temp);
            allChunks.Add(chunk.chunkId, chunk);
            battleChunksPerRowTeam.Add(new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            }, chunk.chunkId);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = allChunks,
                battleChunksPerRowTeam = battleChunksPerRowTeam,
                moveLeft = new NativeList<long>(100, Allocator.TempJob),
                moveRight = new NativeList<long>(100, Allocator.TempJob),
                moveToDifferentChunk = new NativeList<long>(100, Allocator.TempJob)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM3_5_BasicChunkMovement>();

            var backupPlanDataHolder = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity);
            var moveLeft = backupPlanDataHolder.moveLeft;
            var moveRight = backupPlanDataHolder.moveRight;
            var moveToDifferentChunk = backupPlanDataHolder.moveToDifferentChunk;
            Assert.AreEqual(1, moveLeft.Length);
            Assert.AreEqual(1, moveRight.Length);
            Assert.AreEqual(4, moveToDifferentChunk.Length);
            Assert.AreEqual(1, moveLeft[0]);
            Assert.AreEqual(6, moveRight[0]);
        }
    }
}