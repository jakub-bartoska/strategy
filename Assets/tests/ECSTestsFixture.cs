using NUnit.Framework;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.LowLevel;

namespace tests
{
    public class ECSTestsFixture
    {
        private bool jobsDebuggerWasEnabled;
        private PlayerLoopSystem previousPlayerLoop;
        private World? previousWorld;

        private World? world;

        protected World World => this.world!;

        protected WorldUnmanaged WorldUnmanaged => this.World!.Unmanaged;

        protected EntityManager manager { get; private set; }

        protected EntityManager.EntityManagerDebug ManagerDebug { get; private set; }

        protected void UpdateSystem<T>() where T : unmanaged, ISystem
        {
            World.GetExistingSystem<T>().Update(World.Unmanaged);
        }

        protected SystemHandle CreateSystem<T>() where T : unmanaged, ISystem => World.CreateSystem<T>();

        protected Entity CreateEntity(params ComponentType[] types) => manager.CreateEntity(types);

        protected void CreateEntityCommandBufferSystem()
        {
            World.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        [SetUp]
        public virtual void Setup()
        {
            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            this.previousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            this.previousWorld = World.DefaultGameObjectInjectionWorld;
            this.world = World.DefaultGameObjectInjectionWorld = new World("Test World");
            this.World.UpdateAllocatorEnableBlockFree = true;
            this.manager = this.World.EntityManager;
            this.ManagerDebug = new EntityManager.EntityManagerDebug(this.manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            this.jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up systems before calling CheckInternalConsistency because we might have filters etc
            // holding on SharedComponentData making checks fail
            while (this.World.Systems.Count > 0)
            {
                this.World.DestroySystemManaged(this.World.Systems[0]);
            }

            this.ManagerDebug.CheckInternalConsistency();
            this.World.Dispose();
            World.DefaultGameObjectInjectionWorld = this.previousWorld!;

            JobsUtility.JobDebuggerEnabled = this.jobsDebuggerWasEnabled;

            PlayerLoop.SetPlayerLoop(this.previousPlayerLoop);
        }
    }
}