using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using MercLord.Player.Inventory;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleResultBuilderTests
    {
        [Test]
        public void BuildAddsPlayerPartyAndArmyUpdatesFromLivingSquadMembers()
        {
            var world = World.Create();

            try
            {
                CreateSquad(world, squadId: 20, unitConfigId: 502, BattleTeamType.Attacker, memberCount: 2);
                CreateSquad(world, squadId: 10, unitConfigId: 501, BattleTeamType.Attacker, memberCount: 3);
                CreateMember(world, squadId: 20, dead: false);
                CreateMember(world, squadId: 10, dead: false);
                CreateMember(world, squadId: 10, dead: true);
                CreateMember(world, squadId: 10, dead: false);
                CreatePlayer(world, dead: false);
                world.Commit();

                var session = CreateSession(
                    world,
                    new BattleArmyData
                    {
                        ArmyId = 100,
                        FactionId = 4,
                        CellId = 21,
                        TargetCellId = 22,
                        IsPlayerParty = true
                    },
                    new BattleArmyData());

                var result = new BattleResultBuilder().Build(session, BattleOutcome.AttackerVictory, winnerFactionId: 4);

                Assert.IsTrue(result.PlayerSurvived);
                Assert.IsTrue(result.HasPlayerPartyUpdate);
                Assert.AreEqual(2, result.PlayerParty.Length);
                Assert.AreEqual(501, result.PlayerParty[0].UnitConfigId);
                Assert.AreEqual(2, result.PlayerParty[0].Count);
                Assert.AreEqual(502, result.PlayerParty[1].UnitConfigId);
                Assert.AreEqual(1, result.PlayerParty[1].Count);

                Assert.AreEqual(1, result.ArmyUpdates.Length);
                Assert.AreEqual(100, result.ArmyUpdates[0].ArmyId);
                Assert.IsFalse(result.ArmyUpdates[0].RemoveArmy);
                Assert.AreEqual(4, result.ArmyUpdates[0].FactionId);
                Assert.AreEqual(21, result.ArmyUpdates[0].CellId);
                Assert.AreEqual(22, result.ArmyUpdates[0].TargetCellId);
                Assert.AreEqual(2, result.ArmyUpdates[0].Squads.Length);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildRemovesGlobalArmyWhenAllSquadMembersAreDead()
        {
            var world = World.Create();

            try
            {
                CreateSquad(world, squadId: 10000, unitConfigId: 601, BattleTeamType.Defender, memberCount: 1);
                CreateMember(world, squadId: 10000, dead: true);
                CreatePlayer(world, dead: true);
                world.Commit();

                var session = CreateSession(
                    world,
                    new BattleArmyData(),
                    new BattleArmyData
                    {
                        ArmyId = 200,
                        FactionId = 8,
                        TargetCellId = 34
                    },
                    sourceCellId: 33);

                var result = new BattleResultBuilder().Build(session, BattleOutcome.AttackerVictory, winnerFactionId: 4);

                Assert.IsFalse(result.PlayerSurvived);
                Assert.AreEqual(1, result.ArmyUpdates.Length);
                Assert.AreEqual(200, result.ArmyUpdates[0].ArmyId);
                Assert.IsTrue(result.ArmyUpdates[0].RemoveArmy);
                Assert.AreEqual(8, result.ArmyUpdates[0].FactionId);
                Assert.AreEqual(33, result.ArmyUpdates[0].CellId);
                Assert.AreEqual(34, result.ArmyUpdates[0].TargetCellId);
                Assert.IsEmpty(result.ArmyUpdates[0].Squads);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildAddsCreditsAndLootWhenPlayerSurvivesAndWins()
        {
            var world = World.Create();
            using var rewardConfigs = CreateRewardConfigs();

            try
            {
                CreatePlayer(world, dead: false, BattleTeamType.Attacker);
                world.Commit();

                var session = CreateSession(
                    world,
                    new BattleArmyData(),
                    new BattleArmyData(),
                    sourceCellId: 5,
                    seed: 77);

                var result = new BattleResultBuilder(rewardConfigs.Database)
                    .Build(session, BattleOutcome.AttackerVictory, winnerFactionId: 4);

                Assert.AreEqual(77, result.CreditsReward);
                Assert.AreEqual(2, result.Loot.Length);
                for (var lootIndex = 0; lootIndex < result.Loot.Length; lootIndex++)
                {
                    Assert.AreEqual(rewardConfigs.Item.Id, result.Loot[lootIndex].ItemConfigId);
                    Assert.GreaterOrEqual(result.Loot[lootIndex].Amount, 2);
                    Assert.LessOrEqual(result.Loot[lootIndex].Amount, 4);
                    Assert.AreEqual(ItemInstance.DurabilityNotTracked, result.Loot[lootIndex].Durability);
                }
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildDoesNotAddRewardsWhenPlayerLoses()
        {
            var world = World.Create();
            using var rewardConfigs = CreateRewardConfigs();

            try
            {
                CreatePlayer(world, dead: false, BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world, new BattleArmyData(), new BattleArmyData());
                var result = new BattleResultBuilder(rewardConfigs.Database)
                    .Build(session, BattleOutcome.AttackerVictory, winnerFactionId: 4);

                Assert.AreEqual(0, result.CreditsReward);
                Assert.IsEmpty(result.Loot);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildAddsWinnerInfluenceChangeFromConfig()
        {
            var world = World.Create();
            using var rewardConfigs = CreateRewardConfigs();

            try
            {
                world.Commit();

                var session = CreateSession(
                    world,
                    new BattleArmyData(),
                    new BattleArmyData(),
                    sourceCellId: 42);

                var result = new BattleResultBuilder(rewardConfigs.Database)
                    .Build(session, BattleOutcome.DefenderVictory, winnerFactionId: 8);

                Assert.AreEqual(1, result.InfluenceChanges.Length);
                Assert.AreEqual(42, result.InfluenceChanges[0].CellId);
                Assert.AreEqual(8, result.InfluenceChanges[0].FactionId);
                Assert.AreEqual(12.5f, result.InfluenceChanges[0].Amount);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(
            World world,
            BattleArmyData attacker,
            BattleArmyData defender,
            int sourceCellId = 1,
            int seed = 0)
        {
            return new BattleSession(
                new BattleGenerationRequest
                {
                    Seed = seed,
                    SourceCellId = sourceCellId
                },
                new BattleModel
                {
                    Width = 1,
                    Height = 1,
                    Attacker = attacker,
                    Defender = defender
                },
                world);
        }

        private static void CreateSquad(
            World world,
            int squadId,
            int unitConfigId,
            BattleTeamType team,
            int memberCount)
        {
            var squad = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(squad, new SquadComponent
            {
                SquadId = squadId,
                UnitConfigId = unitConfigId,
                FactionId = team == BattleTeamType.Attacker ? 4 : 8,
                Team = team,
                MemberCount = memberCount
            });
        }

        private static void CreateMember(World world, int squadId, bool dead)
        {
            var member = world.CreateEntity();
            world.GetStash<SquadMemberComponent>().Set(member, new SquadMemberComponent
            {
                SquadId = squadId,
                SlotIndex = 0,
                SquadSize = 1
            });
            world.GetStash<HealthComponent>().Set(member, new HealthComponent
            {
                Current = dead ? 0 : 10,
                Max = 10
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(member, new DeadComponent());
            }
        }

        private static void CreatePlayer(World world, bool dead, BattleTeamType team = BattleTeamType.Attacker)
        {
            var player = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(player, new PlayerControlledComponent());
            world.GetStash<TeamComponent>().Set(player, new TeamComponent
            {
                Value = team
            });
            world.GetStash<HealthComponent>().Set(player, new HealthComponent
            {
                Current = dead ? 0 : 10,
                Max = 10
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(player, new DeadComponent());
            }
        }

        private static RewardConfigSet CreateRewardConfigs()
        {
            var configSet = new RewardConfigSet();
            var database = configSet.Create<ConfigDatabase>();
            var simulation = configSet.CreateConfig<BattleSimulationConfig>(10, "Rewards");
            var lootTable = configSet.CreateConfig<LootTableConfig>(11, "Loot");
            var item = configSet.CreateConfig<ItemConfig>(12, "Scrap");
            var lootEntry = WithField(default(LootEntry), "item", item);
            lootEntry = WithField(lootEntry, "minCount", 2);
            lootEntry = WithField(lootEntry, "maxCount", 4);
            lootEntry = WithField(lootEntry, "weight", 1f);

            SetField(lootTable, "entries", new[] { lootEntry });
            SetField(simulation, "victoryCreditsReward", 77);
            SetField(simulation, "victoryLootTable", lootTable);
            SetField(simulation, "victoryLootRolls", 2);
            SetField(simulation, "victoryInfluenceReward", 12.5f);
            SetField(database, "items", new[] { item });
            SetField(database, "lootTables", new[] { lootTable });
            SetField(database, "battleSimulation", simulation);

            configSet.Database = database;
            configSet.Item = item;
            return configSet;
        }

        private static T WithField<T>(T value, string fieldName, object fieldValue)
            where T : struct
        {
            object boxed = value;
            SetField(boxed, fieldName, fieldValue);
            return (T)boxed;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }

        private sealed class RewardConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public ConfigDatabase Database { get; set; }
            public ItemConfig Item { get; set; }

            public T Create<T>()
                where T : ScriptableObject
            {
                var config = ScriptableObject.CreateInstance<T>();
                assets.Add(config);
                return config;
            }

            public T CreateConfig<T>(int id, string displayName)
                where T : IdentifiedConfig
            {
                var config = Create<T>();
                SetField(config, "id", id);
                SetField(config, "displayName", displayName);
                return config;
            }

            public void Dispose()
            {
                for (var assetIndex = assets.Count - 1; assetIndex >= 0; assetIndex--)
                {
                    if (assets[assetIndex] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(assets[assetIndex]);
                    }
                }
            }
        }
    }
}
