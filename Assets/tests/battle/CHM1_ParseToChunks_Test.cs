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
    public class CHM1_ParseToChunks_Test : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<CHM1_ParseToChunks>();
            var entity = CreateEntity();
            var battleMapStateMarker = new BattleMapStateMarker();
            manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void team1_team2()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();
            var positions = DataHolderUtils.createPositions(new[]
            {
                DataHolderUtils.createBattalion(new float3(20, 0, 0), Team.TEAM2, 3),
                DataHolderUtils.createBattalion(new float3(10, 0, 0), Team.TEAM1, 2),
                DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM1, 1),
            });
            dataHolder.positions = positions;

            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                lastChunkId = 0,
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);

            UpdateSystem<CHM1_ParseToChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunksPerRowTeam;
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 1
            };
            Assert.AreEqual(1, battleChunks.CountValuesForKey(team1Key));
            Assert.AreEqual(1, battleChunks.CountValuesForKey(team2Key));
            var team1Id = DataHolderUtils.getChunkByTeamPosition(singletonEntity, Team.TEAM1, manager);
            var team1 = allChunks[team1Id];
            Assert.AreEqual((int) Team.TEAM1, (int) team1.team);
            Assert.AreEqual(0, team1.chunkId);
            Assert.AreEqual(false, team1.leftFighting);
            Assert.AreEqual(true, team1.rightFighting);
            Assert.AreEqual(2, team1.battalions.Length);
            Assert.AreEqual(1, team1.battalions[0]);
            Assert.AreEqual(2, team1.battalions[1]);
            Assert.AreEqual(1, team1.rowId);
            //1st battalion is 0, - size = -1.5
            Assert.AreEqual(-1.5, team1.startX);
            //last team1 battalion is 10, but chunk ends at 1st enemy 2  => 20 - 1.5 = 18.5
            Assert.AreEqual(18.5, team1.endX);

            var team2Id = DataHolderUtils.getChunkByTeamPosition(singletonEntity, Team.TEAM2, manager);
            var team2 = allChunks[team2Id];
            Assert.AreEqual((int) Team.TEAM2, (int) team2.team);
            Assert.AreEqual(1, team2.chunkId);
            Assert.AreEqual(true, team2.leftFighting);
            Assert.AreEqual(false, team2.rightFighting);
            Assert.AreEqual(1, team2.battalions.Length);
            Assert.AreEqual(3, team2.battalions[0]);
            Assert.AreEqual(1, team2.rowId);
            Assert.AreEqual(18.5, team2.startX);
            Assert.AreEqual(21.5, team2.endX);
        }

        [Test]
        public void team2_team1_team2()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = DataHolderUtils.createBasicDataholder();
            var positions = DataHolderUtils.createPositions(new[]
            {
                DataHolderUtils.createBattalion(new float3(20, 0, 0), Team.TEAM2, 3),
                DataHolderUtils.createBattalion(new float3(10, 0, 0), Team.TEAM1, 2),
                DataHolderUtils.createBattalion(new float3(0, 0, 0), Team.TEAM2, 1),
            });
            dataHolder.positions = positions;

            manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                allChunks = new NativeHashMap<long, BattleChunk>(100, Allocator.Temp),
                lastChunkId = 0,
                battleChunksPerRowTeam = new NativeParallelMultiHashMap<TeamRow, long>(100, Allocator.Temp)
            };

            manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM1_ParseToChunks>();

            var allChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).allChunks;
            var battleChunks = manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunksPerRowTeam;
            var team1Key = new TeamRow
            {
                team = Team.TEAM1,
                rowId = 1
            };
            var team2Key = new TeamRow
            {
                team = Team.TEAM2,
                rowId = 1
            };
            Assert.AreEqual(1, battleChunks.CountValuesForKey(team1Key));
            Assert.AreEqual(2, battleChunks.CountValuesForKey(team2Key));
            var team1id = DataHolderUtils.getChunkByTeamPosition(singletonEntity, Team.TEAM1, manager);
            var team1 = allChunks[team1id];
            Assert.AreEqual((int) Team.TEAM1, (int) team1.team);
            Assert.AreEqual(true, team1.leftFighting);
            Assert.AreEqual(true, team1.rightFighting);
            Assert.AreEqual(1, team1.battalions.Length);
            Assert.AreEqual(2, team1.battalions[0]);
            Assert.AreEqual(1, team1.rowId);
            Assert.AreEqual(8.5, team1.startX);
            Assert.AreEqual(18.5, team1.endX);

            var team2_1id = DataHolderUtils.getChunkByTeamPosition(singletonEntity, Team.TEAM2, manager, 1);
            var team2_1 = allChunks[team2_1id];
            Assert.AreEqual((int) Team.TEAM2, (int) team2_1.team);
            Assert.AreEqual(false, team2_1.leftFighting);
            Assert.AreEqual(true, team2_1.rightFighting);
            Assert.AreEqual(1, team2_1.battalions.Length);
            Assert.AreEqual(1, team2_1.battalions[0]);
            Assert.AreEqual(1, team2_1.rowId);
            Assert.AreEqual(-1.5, team2_1.startX);
            Assert.AreEqual(8.5, team2_1.endX);

            var team2_2id = DataHolderUtils.getChunkByTeamPosition(singletonEntity, Team.TEAM2, manager, 0);
            var team2_2 = allChunks[team2_2id];
            Assert.AreEqual((int) Team.TEAM2, (int) team2_2.team);
            Assert.AreEqual(true, team2_2.leftFighting);
            Assert.AreEqual(false, team2_2.rightFighting);
            Assert.AreEqual(1, team2_2.battalions.Length);
            Assert.AreEqual(3, team2_2.battalions[0]);
            Assert.AreEqual(1, team2_2.rowId);
            Assert.AreEqual(18.5, team2_2.startX);
            Assert.AreEqual(21.5, team2_2.endX);
        }
    }
}