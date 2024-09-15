using System;
using component._common.system_switchers;
using component.battle.battalion;
using component.battle.battalion.data_holders;
using system.battle.system_groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace system.battle.battalion.split
{
    [UpdateInGroup(typeof(BattleExecutionSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public partial struct SP_OrderSoldiers : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<SoldierPositions>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var soldierPositions = SystemAPI.GetSingleton<SoldierPositions>();

            new AdjustSoldierPositionJob
                {
                    soldierPositions1 = soldierPositions.positions[1],
                    soldierPositions2 = soldierPositions.positions[2],
                    soldierPositions3 = soldierPositions.positions[3],
                    soldierPositions4 = soldierPositions.positions[4],
                    soldierPositions5 = soldierPositions.positions[5],
                    soldierPositions6 = soldierPositions.positions[6],
                    soldierPositions7 = soldierPositions.positions[7],
                    soldierPositions8 = soldierPositions.positions[8],
                    soldierPositions9 = soldierPositions.positions[9],
                    soldierPositions10 = soldierPositions.positions[10]
                }.Schedule(state.Dependency)
                .Complete();
        }
    }

    [BurstCompile]
    public partial struct AdjustSoldierPositionJob : IJobEntity
    {
        public NativeList<int> soldierPositions1;
        public NativeList<int> soldierPositions2;
        public NativeList<int> soldierPositions3;
        public NativeList<int> soldierPositions4;
        public NativeList<int> soldierPositions5;
        public NativeList<int> soldierPositions6;
        public NativeList<int> soldierPositions7;
        public NativeList<int> soldierPositions8;
        public NativeList<int> soldierPositions9;
        public NativeList<int> soldierPositions10;

        private void Execute(BattalionMarker battalionMarker, ref DynamicBuffer<BattalionSoldiers> soldiers,
            EnabledRefRW<SoldierReorderMarker> reorderMarker)
        {
            reorderMarker.ValueRW = false;

            if (soldiers.Length == 0)
            {
                return;
            }

            var finalPositions = pickSoldierPositions(soldiers.Length);

            var currentPositions = new NativeList<int>(soldiers.Length, Allocator.TempJob);
            foreach (var soldier in soldiers)
            {
                currentPositions.Add(soldier.positionWithinBattalion);
            }

            currentPositions.Sort();
            var resultMap = new NativeHashMap<int, int>(soldiers.Length, Allocator.TempJob);
            for (int i = currentPositions.Length - 1; i >= 0; i--)
            {
                resultMap.Add(currentPositions[i], finalPositions[i]);
            }

            for (int i = 0; i < soldiers.Length; i++)
            {
                var oldSoldier = soldiers[i];
                var desiredPosition = resultMap[oldSoldier.positionWithinBattalion];
                if (desiredPosition == oldSoldier.positionWithinBattalion) continue;

                oldSoldier.positionWithinBattalion = desiredPosition;
                soldiers[i] = oldSoldier;
            }

            currentPositions.Dispose();
            resultMap.Dispose();
        }

        private NativeList<int> pickSoldierPositions(int soldierCount)
        {
            return soldierCount switch
            {
                1 => soldierPositions1,
                2 => soldierPositions2,
                3 => soldierPositions3,
                4 => soldierPositions4,
                5 => soldierPositions5,
                6 => soldierPositions6,
                7 => soldierPositions7,
                8 => soldierPositions8,
                9 => soldierPositions9,
                10 => soldierPositions10,
                _ => throw new Exception("Missing soldier positions for " + soldierCount + " soldiers")
            };
        }
    }
}