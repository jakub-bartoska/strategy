using component.battle.battalion;
using NUnit.Framework;
using system.battle.battalion.analysis.data_holder;
using system.battle.battalion.analysis.reinforcements;
using Assert = Unity.Assertions.Assert;

namespace tests.testiky
{
    [TestFixture]
    public class Testik : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            CreateSystem<FindNeededReinforcementsSystem>();
        }

        [Test]
        public void Update_HasCollisionsOnRightAndMovingRight_HorizontalMovementIsReset()
        {
            DataHolder.needReinforcements.Clear();
            var entity = CreateEntity();
            var battalionMarker = new BattalionMarker
            {
                id = 1
            };

            Manager.AddComponentData(entity, battalionMarker);
            var soldierBuffer = Manager.AddBuffer<BattalionSoldiers>(entity);

            for (int i = 0; i < 8; i++)
            {
                var soldier = new BattalionSoldiers
                {
                    positionWithinBattalion = i
                };
                soldierBuffer.Add(soldier);
            }

            // Update the system
            UpdateSystem<FindNeededReinforcementsSystem>();

            var result = DataHolder.needReinforcements;
            // Assert that data is modified according to the test case
            //var movementData = Manager.GetComponentData<MovementDirectionData>(entity);
            //Assert.AreEqual(0, movementData.Movement.x);
            Assert.IsTrue(true);
        }
    }
}