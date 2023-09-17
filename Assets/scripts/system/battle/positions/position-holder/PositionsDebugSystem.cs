using component;
using component._common.system_switchers;
using component.pathfinding;
using component.soldier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace system.positions.position_holder
{
    [BurstCompile]
    [UpdateAfter(typeof(ParsePositionsToPositionHolderSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PositionsDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
            state.RequireForUpdate<PositionHolderConfig>();
            state.RequireForUpdate<BattleMapStateMarker>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            //todo oddelat
            state.Enabled = false;
            var positionHolder = SystemAPI.GetSingleton<PositionHolderConfig>();

            drawGrid(positionHolder);

            new drawLineToClosestEnemy().Run();
        }

        private void drawGrid(PositionHolderConfig positionHolderConfig)
        {
            var min = positionHolderConfig.minSquarePosition;
            var max = positionHolderConfig.maxSquarePosition;
            var step = positionHolderConfig.oneSquareSize;
            var xSteps = math.abs(min.x - max.x) / step + 1;
            for (var i = 0; i < xSteps; i++)
            {
                var currentx = min.x + i * step;
                Debug.DrawLine(new Vector3(currentx, 1, min.y), new Vector3(currentx, 1, max.y), Color.black);
            }

            var zSteps = math.abs(min.y - max.y) / step + 1;
            for (var i = 0; i < zSteps; i++)
            {
                var currentz = min.y + i * step;
                Debug.DrawLine(new Vector3(min.x, 1, currentz), new Vector3(max.x, 1, currentz), Color.black);
            }
        }
    }

    public partial struct drawLineToClosestEnemy : IJobEntity
    {
        private void Execute(SoldierStatus soldierStatus, ClosestEnemy closestEnemy, LocalTransform transformAspect)
        {
            var color = soldierStatus.team == Team.TEAM1 ? Color.yellow : Color.cyan;
            if (closestEnemy.closestEnemyId != -1)
            {
                Debug.DrawLine(transformAspect.Position, closestEnemy.closestEnemyPosition, color);
            }
        }
    }
}