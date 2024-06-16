using component;
using component._common.system_switchers;
using component.battle.battalion;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battalion.execution.movement.wait_for_soldiers
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    public partial struct WFS1_RemoveMark : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var soldiersToCheck = new NativeHashMap<long, WFS1_Info>(10000, Allocator.TempJob);
            new CollectBattleUnitPositionsJob
                {
                    soldiersToCheck = soldiersToCheck
                }.Schedule(state.Dependency)
                .Complete();
            if (soldiersToCheck.IsEmpty)
            {
                soldiersToCheck.Dispose();
                return;
            }

            new AddSoldierPositionJob
                {
                    soldiersToCheck = soldiersToCheck
                }.Schedule(state.Dependency)
                .Complete();

            var battalionIdsToRemoveMark = new NativeHashMap<long, Entity>(soldiersToCheck.Count, Allocator.TempJob);

            //collect all battalion ids
            foreach (var pair in soldiersToCheck)
            {
                battalionIdsToRemoveMark.TryAdd(pair.Value.battalionId, pair.Value.battalionEntity);
            }

            //remove battalions which have at least 1 soldier in long distance
            foreach (var pair in soldiersToCheck)
            {
                if (!battalionIdsToRemoveMark.ContainsKey(pair.Value.battalionId))
                {
                    continue;
                }

                if (math.distance(pair.Value.battalionPosition.x, pair.Value.soldierPosition.x) > (pair.Value.width * 0.25f))
                {
                    battalionIdsToRemoveMark.Remove(pair.Value.battalionId);
                }
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var battalionIdEntity in battalionIdsToRemoveMark)
            {
                ecb.RemoveComponent<WaitForSoldiersTag>(battalionIdEntity.Value);
            }

            soldiersToCheck.Dispose();
            battalionIdsToRemoveMark.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(WaitForSoldiersTag))]
    public partial struct CollectBattleUnitPositionsJob : IJobEntity
    {
        public NativeHashMap<long, WFS1_Info> soldiersToCheck;

        private void Execute(DynamicBuffer<BattalionSoldiers> soldiers, BattalionMarker battalionMarker, BattalionWidth width, LocalTransform transform, Entity entity)
        {
            foreach (var soldier in soldiers)
            {
                soldiersToCheck.Add(soldier.soldierId, new WFS1_Info
                {
                    battalionId = battalionMarker.id,
                    width = width.value,
                    battalionPosition = transform.Position,
                    battalionEntity = entity
                });
            }
        }
    }

    [BurstCompile]
    public partial struct AddSoldierPositionJob : IJobEntity
    {
        public NativeHashMap<long, WFS1_Info> soldiersToCheck;

        private void Execute(SoldierStatus soldierStatus, LocalTransform transform)
        {
            if (soldiersToCheck.ContainsKey(soldierStatus.index))
            {
                var info = soldiersToCheck[soldierStatus.index];
                info.soldierPosition = transform.Position;
                soldiersToCheck[soldierStatus.index] = info;
            }
        }
    }

    public struct WFS1_Info
    {
        public long battalionId;
        public float width;
        public float3 battalionPosition;
        public float3 soldierPosition;
        public Entity battalionEntity;
    }
}