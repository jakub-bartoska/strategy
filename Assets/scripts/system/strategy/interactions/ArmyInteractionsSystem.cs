using System;
using System.Collections.Generic;
using component._common.general;
using component._common.system_switchers;
using component.config.game_settings;
using component.strategy.army_components;
using component.strategy.general;
using component.strategy.town_components;
using ProjectDawn.Navigation;
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
            var team1 = new NativeParallelMultiHashMap<long, float3>(1000, Allocator.TempJob);
            var team2 = new NativeParallelMultiHashMap<long, float3>(1000, Allocator.TempJob);
            var team1Towns = new NativeParallelMultiHashMap<long, float3>(1000, Allocator.TempJob);
            var team2Towns = new NativeParallelMultiHashMap<long, float3>(1000, Allocator.TempJob);
            var armyCompanies = new NativeParallelMultiHashMap<long, ArmyCompany>(1000, Allocator.TempJob);
            var armyTownPairs = new NativeParallelMultiHashMap<long, long>(1000, Allocator.TempJob);
            var armyDistanceToDestination = new NativeParallelMultiHashMap<long, float>(1000, Allocator.TempJob);
            new ArmyPositionsGatherJob
                {
                    team1 = team1.AsParallelWriter(),
                    team2 = team2.AsParallelWriter(),
                    armySizes = armyCompanies.AsParallelWriter(),
                    armyTownPairs = armyTownPairs.AsParallelWriter(),
                    armyDistanceToDestination = armyDistanceToDestination.AsParallelWriter()
                }.ScheduleParallel(state.Dependency)
                .Complete();
            new TownPositionsGatherJob
                {
                    team1towns = team1Towns.AsParallelWriter(),
                    team2towns = team2Towns.AsParallelWriter(),
                }.ScheduleParallel(state.Dependency)
                .Complete();

            //todo do configu
            var minDistance = 1;
            var interactions = new NativeList<(long, long, InteractionType)>(300, Allocator.TempJob);
            findInteractions(team1, team2, team1Towns, team2Towns, minDistance, interactions);

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


            var onlyMerges = new NativeList<(long, long, InteractionType)>(interactions.Length, Allocator.TempJob);
            foreach (var (army1, army2, interactionType) in interactions)
            {
                if (interactionType != InteractionType.ANY_ARMY_WITH_ARMY_INTERACTION)
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
                    team1 = team1,
                    team2 = team2,
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

        private bool isBattleRunning()
        {
            var armyToSpawnBuffer = SystemAPI.GetSingletonBuffer<ArmyToSpawn>();
            return armyToSpawnBuffer.Length > 0;
        }

        private void findInteractions(
            NativeParallelMultiHashMap<long, float3> team1,
            NativeParallelMultiHashMap<long, float3> team2,
            NativeParallelMultiHashMap<long, float3> team1Towns,
            NativeParallelMultiHashMap<long, float3> team2Towns,
            int minDistance,
            NativeList<(long, long, InteractionType)> interactions)
        {
            //fights
            foreach (var t1 in team1)
            {
                foreach (var t2 in team2)
                {
                    if (math.distance(t1.Value, t2.Value) < minDistance)
                    {
                        interactions.Add((t1.Key, t2.Key, InteractionType.FIGHT));
                    }
                }
            }

            //army merges
            findMergeInteractions(interactions, team1, minDistance);
            findMergeInteractions(interactions, team2, minDistance);

            //enter town
            foreach (var army1 in team1)
            {
                foreach (var town1 in team1Towns)
                {
                    if (math.distance(army1.Value, town1.Value) < minDistance)
                    {
                        interactions.Add((army1.Key, town1.Key, InteractionType.ENTER_TOWN));
                    }
                }

                foreach (var town2 in team2Towns)
                {
                    if (math.distance(army1.Value, town2.Value) < minDistance)
                    {
                        interactions.Add((army1.Key, town2.Key, InteractionType.FIGHT_TOWN));
                    }
                }
            }

            foreach (var army2 in team2)
            {
                foreach (var town1 in team1Towns)
                {
                    if (math.distance(army2.Value, town1.Value) < minDistance)
                    {
                        interactions.Add((army2.Key, town1.Key, InteractionType.FIGHT_TOWN));
                    }
                }

                foreach (var town2 in team2Towns)
                {
                    if (math.distance(army2.Value, town2.Value) < minDistance)
                    {
                        interactions.Add((army2.Key, town2.Key, InteractionType.ENTER_TOWN));
                    }
                }
            }
        }

        private void findMergeInteractions(NativeList<(long, long, InteractionType)> interactions,
            NativeParallelMultiHashMap<long, float3> team, int minDistance)
        {
            foreach (var army1 in team)
            {
                foreach (var army2 in team)
                {
                    //remove duplicity interactions
                    //remove interactions when army2 has the same ID as army1
                    if (army2.Key >= army1.Key)
                    {
                        continue;
                    }

                    if (math.distance(army1.Value, army2.Value) < minDistance)
                    {
                        interactions.Add((army1.Key, army2.Key, InteractionType.ANY_ARMY_WITH_ARMY_INTERACTION));
                    }
                }
            }
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
        public NativeParallelMultiHashMap<long, float3>.ParallelWriter team1;
        public NativeParallelMultiHashMap<long, float3>.ParallelWriter team2;
        public NativeParallelMultiHashMap<long, ArmyCompany>.ParallelWriter armySizes;
        public NativeParallelMultiHashMap<long, long>.ParallelWriter armyTownPairs;
        public NativeParallelMultiHashMap<long, float>.ParallelWriter armyDistanceToDestination;

        private void Execute(ArmyTag tag, LocalTransform localTransform, DynamicBuffer<ArmyCompany> companies,
            TeamComponent team, ArmyMovementStatus movementStatus, IdHolder idHolder, AgentBody agentBody)
        {
            if (team.team == Team.TEAM1)
            {
                team1.Add(idHolder.id, localTransform.Position);
            }
            else
            {
                team2.Add(idHolder.id, localTransform.Position);
            }

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
    public partial struct TownPositionsGatherJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, float3>.ParallelWriter team1towns;
        public NativeParallelMultiHashMap<long, float3>.ParallelWriter team2towns;

        private void Execute(TownTag tag, LocalTransform localTransform, TeamComponent team, IdHolder idHolder)
        {
            if (team.team == Team.TEAM1)
            {
                team1towns.Add(idHolder.id, localTransform.Position);
            }
            else
            {
                team2towns.Add(idHolder.id, localTransform.Position);
            }
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
        public NativeParallelMultiHashMap<long, float3> team1;
        public NativeParallelMultiHashMap<long, float3> team2;
        [ReadOnly] public NativeList<(long, long)> armyEnteringTownList;

        private void Execute(ArmyTag tag, ref DynamicBuffer<ArmyInteraction> interactions, Entity entity,
            DynamicBuffer<Child> childs, ref LocalTransform transform, ref ArmyMovementStatus movementStatus,
            ref DynamicBuffer<ArmyCompany> companies, TeamComponent team, IdHolder idHolder)
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
                        var otherArmyPosition = getMergingArmyPosition(change.Value.Item1, team.team);
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

        private float3 getMergingArmyPosition(long otherArmyId, Team team)
        {
            if (team == Team.TEAM1)
            {
                team1.TryGetFirstValue(otherArmyId, out var position, out _);
                return position;
            }
            else
            {
                team2.TryGetFirstValue(otherArmyId, out var position, out _);
                return position;
            }
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
}