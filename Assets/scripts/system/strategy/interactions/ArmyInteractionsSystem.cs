using System;
using System.Collections.Generic;
using component._common.general;
using component._common.system_switchers;
using component.authoring_pairs.PrefabHolder;
using component.config.game_settings;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.town_components;
using ProjectDawn.Navigation;
using system.strategy.utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace component.strategy.interactions
{
    public partial struct ArmyInteractionsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyToSpawn>();
            state.RequireForUpdate<GamePlayerSettings>();
            state.RequireForUpdate<StrategyMapStateMarker>();
            state.RequireForUpdate<SelectionMarkerState>();
            state.RequireForUpdate<SingletonEntityTag>();
            state.RequireForUpdate<ArmyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //todo predelat
            if (isBattleRunning())
            {
                return;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            //todo 1000 capacity je blbe
            var entities = new NativeParallelMultiHashMap<long, (float3, IdHolder, Team)>(1000, Allocator.TempJob);
            var armyCompanies = new NativeParallelMultiHashMap<long, ArmyCompany>(1000, Allocator.TempJob);
            var armyTownPairs = new NativeParallelMultiHashMap<long, long>(1000, Allocator.TempJob);
            var armyDistanceToDestination = new NativeParallelMultiHashMap<long, float>(1000, Allocator.TempJob);
            new ArmyPositionsGatherJob
                {
                    armySizes = armyCompanies.AsParallelWriter(),
                    armyTownPairs = armyTownPairs.AsParallelWriter(),
                    armyDistanceToDestination = armyDistanceToDestination.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
            new EntitiesCollectorJob
                {
                    entities = entities.AsParallelWriter(),
                }.ScheduleParallel(state.Dependency)
                .Complete();

            //todo do configu
            var minDistance = 1;
            var interactions = new NativeList<(long, long, InteractionType)>(300, Allocator.TempJob);
            findInteractions(entities, minDistance, interactions);

            if (interactions.Length == 0)
            {
                return;
            }

            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();
            var systemSwitch = SystemAPI.GetSingletonRW<SystemStatusHolder>();
            //armyID entering town
            var enterTownInteractions = new NativeList<(long, long)>(100, Allocator.TempJob);
            foreach (var (army1, army2, interactionType) in interactions)
            {
                switch (interactionType)
                {
                    case InteractionType.FIGHT:
                        new SpawnBattleJob
                            {
                                team1ArmyId = army1,
                                team1ArmyHolderType = HolderType.ARMY,
                                team2ArmyId = army2,
                                team2ArmyHolderType = HolderType.ARMY,
                                buffer = SystemAPI.GetSingletonBuffer<ArmyToSpawn>()
                            }.Schedule(state.Dependency)
                            .Complete();
                        blockers.Add(new SystemSwitchBlocker
                        {
                            blocker = Blocker.SPAWN_ARMY
                        });
                        blockers.Add(new SystemSwitchBlocker
                        {
                            blocker = Blocker.AUTO_ADD_BLOCKERS
                        });
                        systemSwitch.ValueRW.desiredStatus = SystemStatus.BATTLE;
                        return;
                    case InteractionType.FIGHT_TOWN:
                        new SpawnBattleJob
                            {
                                team1ArmyId = army1,
                                team1ArmyHolderType = HolderType.ARMY,
                                team2ArmyId = army2,
                                team2ArmyHolderType = HolderType.TOWN,
                                buffer = SystemAPI.GetSingletonBuffer<ArmyToSpawn>()
                            }.Schedule(state.Dependency)
                            .Complete();
                        blockers.Add(new SystemSwitchBlocker
                        {
                            blocker = Blocker.SPAWN_ARMY
                        });
                        blockers.Add(new SystemSwitchBlocker
                        {
                            blocker = Blocker.AUTO_ADD_BLOCKERS
                        });
                        systemSwitch.ValueRW.desiredStatus = SystemStatus.BATTLE;
                        return;
                    case InteractionType.ENTER_TOWN:
                        if (armyTownPairs.TryGetFirstValue(army1, out var townId, out _) && army2 == townId)
                        {
                            enterTownInteractions.Add((army1, townId));
                        }

                        break;
                    default:
                        continue;
                }
            }

            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var minorsToChangeTeam = selectOutMinorCaptures(interactions);
            new ChangeTeamForMinors
                {
                    minorsToChangeTeam = minorsToChangeTeam,
                    ecb = ecb,
                    prefabHolder = prefabHolder
                }.Schedule(state.Dependency)
                .Complete();

            var onlyMerges = new NativeList<(long, long, InteractionType)>(interactions.Length, Allocator.TempJob);
            foreach (var (army1, army2, interactionType) in interactions)
            {
                if (interactionType != InteractionType.ANY_ARMY_MERGE)
                {
                    continue;
                }

                var doesNotContainMergeToTown = true;
                foreach (var (armyId, _) in enterTownInteractions)
                {
                    if (armyId == army1 || armyId == army2)
                    {
                        doesNotContainMergeToTown = false;
                        break;
                    }
                }

                if (doesNotContainMergeToTown)
                {
                    onlyMerges.Add((army1, army2, interactionType));
                }
            }

            //now I know there is no fight in interaction List
            var validMerges = new NativeList<(long, long, InteractionType)>(onlyMerges.Length, Allocator.TempJob);
            new ValidateMergesJob
            {
                input = onlyMerges,
                armyDistanceToDestination = armyDistanceToDestination,
                result = validMerges
            }.Run();

            validMerges.Sort(new SortByLowestId());
            var unique = validMerges.ToArray(Allocator.TempJob);
            var uniqueCount = unique.Unique();
            unique = unique.GetSubArray(0, uniqueCount);
            //todo unique neni unique, muze obsahovat to ze jeden merguje do jednoho a naopak

            var alreadyMerged = new NativeList<long>(unique.Length * 2, Allocator.TempJob);
            var result = new NativeList<(long, long, InteractionType)>(Allocator.TempJob);
            foreach (var merge in unique)
            {
                if (alreadyMerged.Contains(merge.Item1) || alreadyMerged.Contains(merge.Item2))
                {
                    continue;
                }

                alreadyMerged.Add(merge.Item1);
                alreadyMerged.Add(merge.Item2);
                result.Add(merge);
            }

            var changes = new NativeHashMap<long, (long, InteractionType)>(result.Length, Allocator.TempJob);
            foreach (var merge in result)
            {
                changes.Add(merge.Item2, (merge.Item1, merge.Item3));
            }

            new MergeArmiesJob
                {
                    changes = changes,
                    ecb = ecb.AsParallelWriter(),
                    armyCompanies = armyCompanies,
                    entities = entities,
                    armyEnteringTownList = enterTownInteractions
                }.Schedule(state.Dependency)
                .Complete();

            new UpdateTownCompanies
                {
                    armyEnteringTownList = enterTownInteractions,
                    armyCompanies = armyCompanies
                }.Schedule(state.Dependency)
                .Complete();
        }

        private NativeArray<long> selectOutMinorCaptures(NativeList<(long, long, InteractionType)> interactions)
        {
            var captures = new NativeHashMap<long, (long, InteractionType)>(interactions.Length, Allocator.TempJob);
            foreach (var (armyId, secondEntityId, interaction) in interactions)
            {
                if (interaction != InteractionType.CAPTURE_MINOR) continue;

                captures.Add(secondEntityId, (armyId, interaction));
            }

            foreach (var (armyId, secondEntityId, interaction) in interactions)
            {
                if (interaction != InteractionType.DEFEND_MINOR) continue;
                captures.Remove(secondEntityId);
            }

            return captures.GetKeyArray(Allocator.TempJob);
        }

        private bool isBattleRunning()
        {
            var armyToSpawnBuffer = SystemAPI.GetSingletonBuffer<ArmyToSpawn>();
            return armyToSpawnBuffer.Length > 0;
        }

        private void findInteractions(
            NativeParallelMultiHashMap<long, (float3, IdHolder, Team)> entities,
            int minDistance,
            NativeList<(long, long, InteractionType)> interactions)
        {
            var keys = entities.GetKeyArray(Allocator.TempJob);
            foreach (var key1 in keys)
            {
                entities.TryGetFirstValue(key1, out var item1, out _);
                if (item1.Item2.type != HolderType.ARMY)
                {
                    continue;
                }

                foreach (var key2 in keys)
                {
                    entities.TryGetFirstValue(key2, out var item2, out _);
                    if (item1.Item2.id == item2.Item2.id) continue;
                    if (item2.Item2.type == HolderType.ARMY && item1.Item2.id > item2.Item2.id) continue;

                    if (math.distance(item1.Item1, item2.Item1) < minDistance)
                    {
                        var interactionType = getIntercations(item1, item2);
                        interactions.Add((key1, key2, interactionType));
                    }
                }
            }
        }

        private InteractionType getIntercations(
            (float3, IdHolder, Team) entity1,
            (float3, IdHolder, Team) entity2)
        {
            switch (entity2.Item2.type)
            {
                case HolderType.ARMY:
                    return getArmyInteraction(entity1, entity2);
                case HolderType.TOWN:
                    return getTownInteraction(entity1, entity2);
                case HolderType.MILL:
                case HolderType.GOLD_MINE:
                case HolderType.STONE_MINE:
                case HolderType.LUMBERJACK_HUT:
                    return getMinorInteraction(entity1, entity2);
                default:
                    throw new Exception("unknown interaction type" + entity2.Item2.type);
            }
        }

        private InteractionType getArmyInteraction((float3, IdHolder, Team) entity1,
            (float3, IdHolder, Team) entity2)
        {
            if (entity1.Item3 == entity2.Item3)
            {
                return InteractionType.ANY_ARMY_MERGE;
            }

            return InteractionType.FIGHT;
        }

        private InteractionType getTownInteraction((float3, IdHolder, Team) entity1, (float3, IdHolder, Team) entity2)
        {
            if (entity1.Item3 == entity2.Item3)
            {
                return InteractionType.ENTER_TOWN;
            }

            return InteractionType.FIGHT_TOWN;
        }

        private InteractionType getMinorInteraction((float3, IdHolder, Team) entity1, (float3, IdHolder, Team) entity2)
        {
            if (entity1.Item3 == entity2.Item3)
            {
                return InteractionType.DEFEND_MINOR;
            }

            return InteractionType.CAPTURE_MINOR;
        }

        public class SortByLowestId : IComparer<(long, long, InteractionType)>
        {
            public int Compare((long, long, InteractionType) e1, (long, long, InteractionType) e2)
            {
                var res = e1.Item1.CompareTo(e2.Item1);

                if (res != 0)
                {
                    return res;
                }

                res = e1.Item2.CompareTo(e2.Item2);
                if (res != 0)
                {
                    return res;
                }

                return e1.Item3.CompareTo(e2.Item3);
            }
        }
    }

    [BurstCompile]
    public partial struct ArmyPositionsGatherJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, ArmyCompany>.ParallelWriter armySizes;
        public NativeParallelMultiHashMap<long, long>.ParallelWriter armyTownPairs;
        public NativeParallelMultiHashMap<long, float>.ParallelWriter armyDistanceToDestination;

        private void Execute(ArmyTag tag, LocalTransform localTransform, DynamicBuffer<ArmyCompany> companies,
            TeamComponent team, ArmyMovementStatus movementStatus, IdHolder idHolder, AgentBody agentBody)
        {
            foreach (var armyCompany in companies)
            {
                armySizes.Add(idHolder.id, armyCompany);
            }

            if (movementStatus.movementType == MovementType.ENTER_TOWN)
            {
                armyTownPairs.Add(idHolder.id, movementStatus.targetTownId.Value);
            }

            var distanceToDestination = math.distance(agentBody.Destination, localTransform.Position);
            armyDistanceToDestination.Add(idHolder.id, distanceToDestination);
        }
    }

    [BurstCompile]
    public partial struct EntitiesCollectorJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, (float3, IdHolder, Team)>.ParallelWriter entities;

        private void Execute(LocalTransform localTransform, TeamComponent team, IdHolder idHolder)
        {
            if (idHolder.type == HolderType.TOWN_DEPLOYER) return;
            entities.Add(idHolder.id, (localTransform.Position, idHolder, team.team));
        }
    }

    [BurstCompile]
    public partial struct SpawnBattleJob : IJobEntity
    {
        public long team1ArmyId;
        public HolderType team1ArmyHolderType;
        public long team2ArmyId;
        public HolderType team2ArmyHolderType;
        public DynamicBuffer<ArmyToSpawn> buffer;

        private void Execute(DynamicBuffer<ArmyCompany> companies, TeamComponent team, IdHolder idHolder)
        {
            var matchesArmy1 = idHolderMatches(idHolder, team1ArmyId, team1ArmyHolderType);
            var matchesArmy2 = idHolderMatches(idHolder, team2ArmyId, team2ArmyHolderType);
            if (matchesArmy1 || matchesArmy2)
            {
                foreach (var armyCompany in companies)
                {
                    var armyToSpawn = new ArmyToSpawn
                    {
                        originalArmyId = idHolder.id,
                        originalArmyType = idHolder.type,
                        armyCompanyId = armyCompany.id,
                        team = team.team,
                        armyType = armyCompany.type,
                        count = armyCompany.soldierCount,
                        formation = Formation.NO_FORMATION,
                        distanceBetweenSoldiers = 1
                    };
                    buffer.Add(armyToSpawn);
                }
            }
        }

        private bool idHolderMatches(IdHolder idHolder, long id, HolderType type)
        {
            if (idHolder.type != type && (type != HolderType.TOWN || idHolder.type != HolderType.TOWN_DEPLOYER))
            {
                return false;
            }

            return idHolder.id == id;
        }
    }

    [BurstCompile]
    public partial struct ValidateMergesJob : IJobEntity
    {
        public NativeList<(long, long, InteractionType)> input;
        public NativeParallelMultiHashMap<long, float> armyDistanceToDestination;
        public NativeList<(long, long, InteractionType)> result;

        private void Execute(ArmyTag tag, DynamicBuffer<ArmyInteraction> interactions, IdHolder idHolder)
        {
            foreach (var (id1, id2, _) in input)
            {
                if (id1 != idHolder.id && id2 != idHolder.id)
                {
                    continue;
                }

                validateResultsByArmyInteraction(id1, id2, interactions);
            }
        }

        private void validateResultsByArmyInteraction(long id1, long id2, DynamicBuffer<ArmyInteraction> interactions)
        {
            foreach (var armyInteraction in interactions)
            {
                if (armyInteraction.armyId != id1 && armyInteraction.armyId != id2) continue;

                switch (armyInteraction.interactionType)
                {
                    case InteractionType.MERGE_TOGETHER:
                        armyDistanceToDestination.TryGetFirstValue(id1, out var distanceToDestination1, out _);
                        armyDistanceToDestination.TryGetFirstValue(id2, out var distanceToDestination2, out _);
                        var difference = distanceToDestination1 - distanceToDestination2;

                        //armies are same distance from their destination
                        if (math.abs(difference) < 0.1f)
                        {
                            if (id1 < id2)
                            {
                                result.Add((id1, id2, InteractionType.MERGE_TOGETHER));
                            }
                            else
                            {
                                result.Add((id2, id1, InteractionType.MERGE_TOGETHER));
                            }
                        }
                        //army 2 is closer to distance
                        else if (difference < 0)
                        {
                            //merges army 1 into army 2
                            result.Add((id2, id1, InteractionType.MERGE_ME_INTO));
                        }
                        else
                        {
                            //merges army 2 into army 1
                            result.Add((id1, id2, InteractionType.MERGE_ME_INTO));
                        }

                        break;
                    case InteractionType.MERGE_ME_INTO:
                        if (armyInteraction.armyId == id1)
                        {
                            //merges army 1 into army 2
                            result.Add((id2, id1, InteractionType.MERGE_ME_INTO));
                        }
                        else
                        {
                            //merges army 2 into army 1
                            result.Add((id1, id2, InteractionType.MERGE_ME_INTO));
                        }

                        break;
                    default:
                        throw new Exception("Unknown interaction type");
                }
            }
        }
    }

    [BurstCompile]
    public partial struct MergeArmiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeHashMap<long, (long, InteractionType)> changes;
        public NativeParallelMultiHashMap<long, ArmyCompany> armyCompanies;
        public NativeParallelMultiHashMap<long, (float3, IdHolder, Team)> entities;
        [ReadOnly] public NativeList<(long, long)> armyEnteringTownList;

        private void Execute(ArmyTag tag, ref DynamicBuffer<ArmyInteraction> interactions, Entity entity,
            DynamicBuffer<Child> childs, ref LocalTransform transform, ref ArmyMovementStatus movementStatus,
            ref DynamicBuffer<ArmyCompany> companies, IdHolder idHolder)
        {
            foreach (var (armyId, _) in armyEnteringTownList)
            {
                if (armyId == idHolder.id)
                {
                    foreach (var child in childs)
                    {
                        ecb.DestroyEntity((int) idHolder.id + 1000 + child.Value.Index, child.Value);
                    }

                    ecb.DestroyEntity((int) idHolder.id, entity);
                }
            }

            foreach (var change in changes)
            {
                if (change.Value.Item1 == idHolder.id)
                {
                    foreach (var child in childs)
                    {
                        ecb.DestroyEntity((int) idHolder.id + 1000 + child.Value.Index, child.Value);
                    }

                    ecb.DestroyEntity((int) idHolder.id, entity);
                    return;
                }

                if (change.Key == idHolder.id)
                {
                    foreach (var armyCompany in armyCompanies.GetValuesForKey(change.Value.Item1))
                    {
                        companies.Add(armyCompany);
                    }

                    if (change.Value.Item2 == InteractionType.MERGE_TOGETHER)
                    {
                        var otherArmyPosition = getMergingArmyPosition(change.Value.Item1);
                        var mergeToPosition = (otherArmyPosition + transform.Position) / 2;
                        transform.Position = mergeToPosition;
                    }
                }
            }

            var newResult = new NativeList<ArmyInteraction>(interactions.Length, Allocator.TempJob);
            foreach (var armyInteraction in interactions)
            {
                (long, long)? changeOnInteraction = null;
                foreach (var change in changes)
                {
                    if (armyInteraction.armyId == change.Value.Item1)
                    {
                        changeOnInteraction = (change.Key, armyInteraction.armyId);
                    }
                }

                if (changeOnInteraction.HasValue)
                {
                    if (changeOnInteraction.Value.Item1 == idHolder.id)
                    {
                        continue;
                    }

                    var interactionToAdd = new ArmyInteraction
                    {
                        armyId = changeOnInteraction.Value.Item1,
                        interactionType = armyInteraction.interactionType
                    };
                    newResult.Add(interactionToAdd);

                    if (movementStatus.targetArmyId.HasValue &&
                        movementStatus.targetArmyId.Value == changeOnInteraction.Value.Item2)
                    {
                        movementStatus.targetArmyId = changeOnInteraction.Value.Item1;
                    }
                }
                else
                {
                    newResult.Add(armyInteraction);
                }
            }

            interactions.Clear();
            interactions.AddRange(newResult.AsArray());
        }

        private float3 getMergingArmyPosition(long otherArmyId)
        {
            entities.TryGetFirstValue(otherArmyId, out var position, out _);
            return position.Item1;
        }
    }

    [BurstCompile]
    public partial struct UpdateTownCompanies : IJobEntity
    {
        [ReadOnly] public NativeList<(long, long)> armyEnteringTownList;
        public NativeParallelMultiHashMap<long, ArmyCompany> armyCompanies;

        private void Execute(TownTag townTag, ref DynamicBuffer<ArmyCompany> companies, IdHolder idHolder)
        {
            foreach (var (armyId, townId) in armyEnteringTownList)
            {
                if (townId != idHolder.id)
                {
                    return;
                }

                foreach (var armyCompany in armyCompanies.GetValuesForKey(armyId))
                {
                    companies.Add(armyCompany);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ChangeTeamForMinors : IJobEntity
    {
        [ReadOnly] public NativeArray<long> minorsToChangeTeam;
        [ReadOnly] public PrefabHolder prefabHolder;
        public EntityCommandBuffer ecb;

        private void Execute(IdHolder idHolder, ref TeamComponent team, Entity entity)
        {
            if (!minorsToChangeTeam.Contains(idHolder.id)) return;

            if (team.team == Team.TEAM1)
            {
                team.team = Team.TEAM2;
            }
            else
            {
                team.team = Team.TEAM1;
            }

            var townTeamMarker = SpawnUtils.spawnTeamMarker(ecb, team, entity, prefabHolder);
            team.teamMarker = townTeamMarker;
        }
    }
}