using component;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using component.config.game_settings;
using NUnit.Framework;
using system.battle.battalion.analysis.backup_plans;
using system.battle.utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Assert = Unity.Assertions.Assert;

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
            Manager.AddComponentData(entity, battleMapStateMarker);
        }

        [Test]
        public void team1_team2()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = createBasicDataholder();
            var positions = createPositions(new[]
            {
                createBattalion(new float3(2, 0, 0), Team.TEAM2, 3),
                createBattalion(new float3(1, 0, 0), Team.TEAM1, 2),
                createBattalion(new float3(0, 0, 0), Team.TEAM1, 1),
            });
            dataHolder.positions = positions;

            Manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };

            Manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM1_ParseToChunks>();

            var battleChunks = Manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunks;
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
            var team1 = getChunkByTeamPosition(singletonEntity, Team.TEAM1);
            Assert.AreEqual((int) Team.TEAM1, (int) team1.team);
            Assert.AreEqual(false, team1.leftFighting);
            Assert.AreEqual(true, team1.rightFighting);
            Assert.AreEqual(2, team1.battalions.Length);
            Assert.AreEqual(1, team1.battalions[0]);
            Assert.AreEqual(2, team1.battalions[1]);
            Assert.AreEqual(1, team1.rowId);

            var team2 = getChunkByTeamPosition(singletonEntity, Team.TEAM2);
            Assert.AreEqual((int) Team.TEAM2, (int) team2.team);
            Assert.AreEqual(true, team2.leftFighting);
            Assert.AreEqual(false, team2.rightFighting);
            Assert.AreEqual(1, team2.battalions.Length);
            Assert.AreEqual(3, team2.battalions[0]);
            Assert.AreEqual(1, team2.rowId);
        }

        [Test]
        public void team2_team1_team2()
        {
            var singletonEntity = CreateEntity();

            var dataHolder = createBasicDataholder();
            var positions = createPositions(new[]
            {
                createBattalion(new float3(2, 0, 0), Team.TEAM2, 3),
                createBattalion(new float3(1, 0, 0), Team.TEAM1, 2),
                createBattalion(new float3(0, 0, 0), Team.TEAM2, 1),
            });
            dataHolder.positions = positions;

            Manager.AddComponentData(singletonEntity, dataHolder);

            var backupPlanHolder = new BackupPlanDataHolder
            {
                battleChunks = new NativeParallelMultiHashMap<TeamRow, BattleChunk>(100, Allocator.Temp)
            };

            Manager.AddComponentData(singletonEntity, backupPlanHolder);


            UpdateSystem<CHM1_ParseToChunks>();

            var battleChunks = Manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunks;
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
            var team1 = getChunkByTeamPosition(singletonEntity, Team.TEAM1);
            Assert.AreEqual((int) Team.TEAM1, (int) team1.team);
            Assert.AreEqual(true, team1.leftFighting);
            Assert.AreEqual(true, team1.rightFighting);
            Assert.AreEqual(1, team1.battalions.Length);
            Assert.AreEqual(2, team1.battalions[0]);
            Assert.AreEqual(1, team1.rowId);

            var team2_1 = getChunkByTeamPosition(singletonEntity, Team.TEAM2, 1);
            Assert.AreEqual((int) Team.TEAM2, (int) team2_1.team);
            Assert.AreEqual(false, team2_1.leftFighting);
            Assert.AreEqual(true, team2_1.rightFighting);
            Assert.AreEqual(1, team2_1.battalions.Length);
            Assert.AreEqual(1, team2_1.battalions[0]);
            Assert.AreEqual(1, team2_1.rowId);

            var team2_2 = getChunkByTeamPosition(singletonEntity, Team.TEAM2, 0);
            Assert.AreEqual((int) Team.TEAM2, (int) team2_2.team);
            Assert.AreEqual(true, team2_2.leftFighting);
            Assert.AreEqual(false, team2_2.rightFighting);
            Assert.AreEqual(1, team2_2.battalions.Length);
            Assert.AreEqual(3, team2_2.battalions[0]);
            Assert.AreEqual(1, team2_2.rowId);
        }

        private DataHolder createBasicDataholder()
        {
            var allRowIds = new NativeList<int>(Allocator.Temp);
            for (int i = 0; i < 10; i++)
            {
                allRowIds.Add(i);
            }

            var dataHolder = new DataHolder
            {
                allRowIds = allRowIds
            };
            return dataHolder;
        }

        private BattalionInfo createBattalion(float3 position, Team team, long id = -1)
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

        private NativeParallelMultiHashMap<int, BattalionInfo> createPositions(BattalionInfo[] battalions)
        {
            var result = new NativeParallelMultiHashMap<int, BattalionInfo>(battalions.Length, Allocator.Temp);
            foreach (var soldier in battalions)
            {
                result.Add(1, soldier);
            }

            return result;
        }

        private BattleChunk getChunkByTeamPosition(Entity singletonEntity, Team team, int position = 0, int row = 1)
        {
            var battleChunks = Manager.GetComponentData<BackupPlanDataHolder>(singletonEntity).battleChunks;
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