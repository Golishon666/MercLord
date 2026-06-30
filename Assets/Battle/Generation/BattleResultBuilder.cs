using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using MercLord.Player.Inventory;
using Scellecs.Morpeh;

namespace MercLord.Battle.Generation
{
    public sealed class BattleResultBuilder : IBattleResultBuilder
    {
        private readonly ConfigDatabase configDatabase;

        public BattleResultBuilder(ConfigDatabase configDatabase = null)
        {
            this.configDatabase = configDatabase;
        }

        public BattleResult Build(BattleSession session, BattleOutcome outcome, int winnerFactionId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (outcome == BattleOutcome.None)
            {
                throw new InvalidOperationException("Battle result requires a non-empty outcome.");
            }

            var world = session.World ?? throw new InvalidOperationException("BattleResultBuilder requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleResultBuilder cannot build a result from a disposed Morpeh world.");
            }

            var result = new BattleResult
            {
                Outcome = outcome,
                SourceCellId = session.Request.SourceCellId,
                WinnerFactionId = winnerFactionId,
                PlayerSurvived = IsPlayerSurvived(world)
            };

            var armyUpdates = new List<BattleArmyUpdate>(2);
            ApplyTeamResult(
                result,
                session.Model.Attacker,
                BuildTeamSurvivors(world, BattleTeamType.Attacker),
                armyUpdates);
            ApplyTeamResult(
                result,
                session.Model.Defender,
                BuildTeamSurvivors(world, BattleTeamType.Defender),
                armyUpdates);

            result.ArmyUpdates = armyUpdates.ToArray();
            ApplyRewards(result, session, world);
            ApplyInfluenceChanges(result, session);
            return result;
        }

        private void ApplyRewards(BattleResult result, BattleSession session, World world)
        {
            var simulationConfig = configDatabase?.BattleSimulation;
            if (simulationConfig == null ||
                !result.PlayerSurvived ||
                !DidPlayerWin(result.Outcome, world))
            {
                return;
            }

            result.CreditsReward = simulationConfig.VictoryCreditsReward;
            result.Loot = RollLoot(
                simulationConfig.VictoryLootTable,
                simulationConfig.VictoryLootRolls,
                CreateRewardSeed(session, result.Outcome));
        }

        private void ApplyInfluenceChanges(BattleResult result, BattleSession session)
        {
            var simulationConfig = configDatabase?.BattleSimulation;
            if (simulationConfig == null ||
                simulationConfig.VictoryInfluenceReward <= 0f ||
                result.WinnerFactionId == WorldIds.None ||
                (result.Outcome != BattleOutcome.AttackerVictory &&
                 result.Outcome != BattleOutcome.DefenderVictory))
            {
                return;
            }

            result.InfluenceChanges = new[]
            {
                new BattleInfluenceChange
                {
                    CellId = session.Request.SourceCellId,
                    FactionId = result.WinnerFactionId,
                    Amount = simulationConfig.VictoryInfluenceReward
                }
            };
        }

        private static void ApplyTeamResult(
            BattleResult result,
            BattleArmyData army,
            SquadData[] survivingSquads,
            ICollection<BattleArmyUpdate> armyUpdates)
        {
            if (army == null)
            {
                return;
            }

            if (army.IsPlayerParty)
            {
                result.HasPlayerPartyUpdate = true;
                result.PlayerParty = survivingSquads;
            }

            if (army.ArmyId == WorldIds.None)
            {
                return;
            }

            armyUpdates.Add(new BattleArmyUpdate
            {
                ArmyId = army.ArmyId,
                RemoveArmy = survivingSquads.Length == 0,
                FactionId = army.FactionId,
                CellId = army.CellId == WorldIds.None ? result.SourceCellId : army.CellId,
                TargetCellId = army.TargetCellId,
                Squads = survivingSquads
            });
        }

        private static SquadData[] BuildTeamSurvivors(World world, BattleTeamType team)
        {
            var livingBySquadId = CountLivingSquadMembers(world);
            var squads = new List<SquadSurvivor>();
            var squadFilter = world.Filter
                .With<SquadComponent>()
                .Build();
            var squadComponents = world.GetStash<SquadComponent>();

            try
            {
                foreach (var entity in squadFilter)
                {
                    var squad = squadComponents.Get(entity);
                    if (squad.Team != team)
                    {
                        continue;
                    }

                    livingBySquadId.TryGetValue(squad.SquadId, out var livingCount);
                    if (livingCount <= 0)
                    {
                        continue;
                    }

                    squads.Add(new SquadSurvivor(squad.SquadId, squad.UnitConfigId, livingCount));
                }
            }
            finally
            {
                squadFilter.Dispose();
            }

            squads.Sort((left, right) => left.SquadId.CompareTo(right.SquadId));

            var result = new SquadData[squads.Count];
            for (var squadIndex = 0; squadIndex < squads.Count; squadIndex++)
            {
                result[squadIndex] = new SquadData
                {
                    UnitConfigId = squads[squadIndex].UnitConfigId,
                    Count = squads[squadIndex].Count
                };
            }

            return result;
        }

        private static Dictionary<int, int> CountLivingSquadMembers(World world)
        {
            var livingBySquadId = new Dictionary<int, int>();
            var memberFilter = world.Filter
                .With<SquadMemberComponent>()
                .With<HealthComponent>()
                .Build();
            var members = world.GetStash<SquadMemberComponent>();
            var dead = world.GetStash<DeadComponent>();

            try
            {
                foreach (var entity in memberFilter)
                {
                    if (dead.Has(entity))
                    {
                        continue;
                    }

                    var squadId = members.Get(entity).SquadId;
                    livingBySquadId.TryGetValue(squadId, out var count);
                    livingBySquadId[squadId] = count + 1;
                }
            }
            finally
            {
                memberFilter.Dispose();
            }

            return livingBySquadId;
        }

        private static bool IsPlayerSurvived(World world)
        {
            var playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<HealthComponent>()
                .Build();
            var dead = world.GetStash<DeadComponent>();
            var survived = false;

            try
            {
                foreach (var entity in playerFilter)
                {
                    survived = !dead.Has(entity);
                    break;
                }
            }
            finally
            {
                playerFilter.Dispose();
            }

            return survived;
        }

        private static bool DidPlayerWin(BattleOutcome outcome, World world)
        {
            var playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .Build();
            var teams = world.GetStash<TeamComponent>();

            try
            {
                foreach (var entity in playerFilter)
                {
                    var playerTeam = teams.Get(entity).Value;
                    return (outcome == BattleOutcome.AttackerVictory && playerTeam == BattleTeamType.Attacker) ||
                           (outcome == BattleOutcome.DefenderVictory && playerTeam == BattleTeamType.Defender);
                }
            }
            finally
            {
                playerFilter.Dispose();
            }

            return false;
        }

        private static BattleLootEntry[] RollLoot(
            LootTableConfig lootTable,
            int rollCount,
            int seed)
        {
            if (lootTable == null || rollCount <= 0)
            {
                return Array.Empty<BattleLootEntry>();
            }

            var entries = lootTable.Entries ?? Array.Empty<LootEntry>();
            var totalWeight = CalculateTotalWeight(entries);
            if (totalWeight <= 0f)
            {
                return Array.Empty<BattleLootEntry>();
            }

            var random = new Random(seed);
            var loot = new List<BattleLootEntry>(rollCount);
            for (var rollIndex = 0; rollIndex < rollCount; rollIndex++)
            {
                var entry = PickEntry(entries, totalWeight, random);
                if (entry.Item == null ||
                    entry.MinCount <= 0 ||
                    entry.MaxCount < entry.MinCount)
                {
                    continue;
                }

                loot.Add(new BattleLootEntry
                {
                    ItemConfigId = entry.Item.Id,
                    Amount = random.Next(entry.MinCount, entry.MaxCount + 1),
                    Durability = ItemInstance.DurabilityNotTracked
                });
            }

            return loot.ToArray();
        }

        private static float CalculateTotalWeight(LootEntry[] entries)
        {
            var totalWeight = 0f;
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                if (entries[entryIndex].Item != null && entries[entryIndex].Weight > 0f)
                {
                    totalWeight += entries[entryIndex].Weight;
                }
            }

            return totalWeight;
        }

        private static LootEntry PickEntry(
            LootEntry[] entries,
            float totalWeight,
            Random random)
        {
            var roll = random.NextDouble() * totalWeight;
            var cumulativeWeight = 0.0;
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entry = entries[entryIndex];
                if (entry.Item == null || entry.Weight <= 0f)
                {
                    continue;
                }

                cumulativeWeight += entry.Weight;
                if (roll <= cumulativeWeight)
                {
                    return entry;
                }
            }

            return entries[entries.Length - 1];
        }

        private static int CreateRewardSeed(BattleSession session, BattleOutcome outcome)
        {
            unchecked
            {
                var seed = session.Request.Seed;
                seed = (seed * 397) ^ session.Request.SourceCellId;
                seed = (seed * 397) ^ (int)outcome;
                return seed;
            }
        }

        private readonly struct SquadSurvivor
        {
            public SquadSurvivor(int squadId, int unitConfigId, int count)
            {
                SquadId = squadId;
                UnitConfigId = unitConfigId;
                Count = count;
            }

            public int SquadId { get; }
            public int UnitConfigId { get; }
            public int Count { get; }
        }
    }
}
