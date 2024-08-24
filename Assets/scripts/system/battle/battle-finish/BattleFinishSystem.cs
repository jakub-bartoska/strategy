using System;
using System.Linq;
using component;
using component._common.camera;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.general;
using component.helpers;
using component.strategy.army_components;
using component.strategy.general;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace system.battle.battle_finish
{
    public partial struct BattleFinishSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleMapStateMarker>();
            state.RequireForUpdate<SystemSwitchBlocker>();
            state.RequireForUpdate<CompanyToSpawn>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var systemSwitchHolder = SystemAPI.GetSingletonRW<SystemStatusHolder>();

            if (systemSwitchHolder.ValueRO.desiredStatus != SystemStatus.BATTLE) return;

            //companyId - soldierId
            var team1 = new NativeParallelMultiHashMap<long, int>(10000, Allocator.TempJob);
            var team2 = new NativeParallelMultiHashMap<long, int>(10000, Allocator.TempJob);
            new RemainingSoldiersCollectorJob
                {
                    team1 = team1.AsParallelWriter(),
                    team2 = team2.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            if (team1.Count() != 0 && team2.Count() != 0)
            {
                team1.Dispose();
                team2.Dispose();
                return;
            }

            var companyCounts = new NativeHashMap<long, int>(team1.Count() + team2.Count(), Allocator.TempJob);
            var uniquesTeam1 = team1.GetUniqueKeyArray(Allocator.TempJob);
            var uniquesTeam2 = team2.GetUniqueKeyArray(Allocator.TempJob);

            foreach (var companyId in uniquesTeam1.Item1.GetSubArray(0, uniquesTeam1.Item2))
            {
                companyCounts.Add(companyId, team1.CountValuesForKey(companyId));
            }

            foreach (var companyId in uniquesTeam2.Item1.GetSubArray(0, uniquesTeam2.Item2))
            {
                companyCounts.Add(companyId, team2.CountValuesForKey(companyId));
            }

            var armyToSpawnBuffer = SystemAPI.GetSingletonBuffer<CompanyToSpawn>();
            var fightingArmies = new NativeHashSet<(long, HolderType)>(armyToSpawnBuffer.Length, Allocator.TempJob);
            foreach (var armyToSpawn in armyToSpawnBuffer)
            {
                fightingArmies.Add((armyToSpawn.originalArmyId, armyToSpawn.originalArmyType));
            }

            armyToSpawnBuffer.Clear();

            var damage = SystemAPI.GetSingletonBuffer<Damage>();
            damage.Clear();

            var ecb =
                SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged);
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            var loosingTeam = !team1.Any() ? Team.TEAM1 : Team.TEAM2;
            var battlePosition = new NativeList<float3>(2, Allocator.TempJob);
            new SetProperArmyStateJob
                {
                    ecb = ecb,
                    companyIdCounts = companyCounts,
                    fightingArmies = fightingArmies,
                    battlePosition = battlePosition,
                    loosingTeam = loosingTeam,
                    prefabHolder = prefabHolder
                }.Schedule(state.Dependency)
                .Complete();

            new DeleteAllBattleEntities
                {
                    ecb = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();

            systemSwitchHolder.ValueRW.desiredStatus = SystemStatus.STRATEGY;

            var strategyCamera = SystemAPI.GetSingletonRW<StrategyCamera>();
            strategyCamera.ValueRW.desiredPosition = new float3(battlePosition[0].x,
                strategyCamera.ValueRO.desiredPosition.y, battlePosition[0].z);

            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();
            blockers.Add(new SystemSwitchBlocker
            {
                blocker = Blocker.AUTO_ADD_BLOCKERS
            });

            fightingArmies.Dispose();
            companyCounts.Dispose();
            battlePosition.Dispose();
            uniquesTeam1.Item1.Dispose();
            uniquesTeam2.Item1.Dispose();
            team1.Dispose();
            team2.Dispose();
        }
    }

    [BurstCompile]
    public partial struct RemainingSoldiersCollectorJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, int>.ParallelWriter team1;
        public NativeParallelMultiHashMap<long, int>.ParallelWriter team2;

        private void Execute(SoldierStatus status)
        {
            if (status.team == Team.TEAM1)
            {
                team1.Add(status.companyId, status.index);
            }
            else if (status.team == Team.TEAM2)
            {
                team2.Add(status.companyId, status.index);
            }
        }
    }

    [BurstCompile]
    public partial struct SetProperArmyStateJob : IJobEntity
    {
        public NativeHashSet<(long, HolderType)> fightingArmies;
        public NativeHashMap<long, int> companyIdCounts;
        public EntityCommandBuffer ecb;
        public NativeList<float3> battlePosition;
        public Team loosingTeam;
        public PrefabHolder prefabHolder;

        public void Execute(Entity entity, ref DynamicBuffer<ArmyCompany> companies, IdHolder idHolder, LocalTransform transform, ref TeamComponent team)
        {
            foreach (var (id, type) in fightingArmies)
            {
                if (!idHolderMatches(idHolder, id, type)) continue;

                var oldCompanies = companies.ToNativeArray(Allocator.Temp);
                companies.Clear();

                foreach (var oldCompany in oldCompanies)
                {
                    if (companyIdCounts.TryGetValue(oldCompany.id, out var remainingSoldierCount))
                    {
                        var newArmyCompany = new ArmyCompany
                        {
                            id = oldCompany.id,
                            soldierCount = remainingSoldierCount,
                            type = oldCompany.type
                        };
                        companies.Add(newArmyCompany);
                    }
                }

                battlePosition.Add(transform.Position);

                if (loosingTeam != team.team) return;

                switch (idHolder.type)
                {
                    case HolderType.ARMY:
                        ecb.DestroyEntity(entity);
                        break;
                    case HolderType.TOWN:
                        team.team = team.team == Team.TEAM1 ? Team.TEAM2 : Team.TEAM1;
                        destroyOldTownMarker(team);
                        var townTeamMarker = spawnTeamMarker(team, entity);
                        team.teamMarker = townTeamMarker;
                        break;
                    case HolderType.TOWN_DEPLOYER:
                        team.team = team.team == Team.TEAM1 ? Team.TEAM2 : Team.TEAM1;
                        break;
                    default:
                        throw new Exception("unknown id holder type");
                }
            }
        }

        private void destroyOldTownMarker(TeamComponent team)
        {
            ecb.DestroyEntity(team.teamMarker);
        }

        private Entity spawnTeamMarker(TeamComponent team, Entity townEntity)
        {
            var prefab = team.team == Team.TEAM1
                ? prefabHolder.townTeamMarkerTeam1Prefab
                : prefabHolder.townTeamMarkerTeam2Prefab;
            var townTeamMarker = ecb.Instantiate(prefab);
            ecb.SetName(townTeamMarker, "Town team marker");
            ecb.AddComponent(townTeamMarker, new Parent
            {
                Value = townEntity
            });
            return townTeamMarker;
        }

        private bool idHolderMatches(IdHolder idHolder, long id, HolderType type)
        {
            if (idHolder.type != type && (type != HolderType.TOWN || idHolder.type != HolderType.TOWN_DEPLOYER))
            {
                return false;
            }

            switch (idHolder.type)
            {
                case HolderType.TOWN_DEPLOYER:
                    if (type == HolderType.TOWN_DEPLOYER)
                    {
                        return idHolder.id == id;
                    }

                    if (type == HolderType.TOWN)
                    {
                        return idHolder.id == id + 1;
                    }

                    return false;
                case HolderType.TOWN:
                    if (type == HolderType.TOWN_DEPLOYER)
                    {
                        return idHolder.id == id - 1;
                    }

                    if (type == HolderType.TOWN)
                    {
                        return idHolder.id == id;
                    }

                    return false;
                case HolderType.ARMY:
                    if (type != HolderType.ARMY) return false;
                    return idHolder.id == id;
                default:
                    return false;
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(BattleCleanupTag))]
    public partial struct DeleteAllBattleEntities : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(Entity entity)
        {
            ecb.DestroyEntity(entity.Index, entity);
        }
    }
}