using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Game.StateMachine;
using MercLord.Global.Cells;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleEncounterRequestFactoryTests
    {
        [Test]
        public void CreatePlayerVsArmyBuildsMetadataRichBattleRequest()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel();
            var factory = new BattleEncounterRequestFactory(configSet.Database);

            var request = factory.CreatePlayerVsArmy(
                saveModel,
                opponentArmyId: 42,
                playerIsAttacker: true,
                seed: 1234,
                nearSettlement: true,
                loadScene: false);

            Assert.AreEqual(7, request.SourceCellId);
            Assert.AreEqual(1234, request.Seed);
            Assert.IsTrue(request.NearSettlement);
            Assert.IsFalse(request.LoadScene);

            Assert.IsTrue(request.Attacker.IsPlayerParty);
            Assert.AreEqual(configSet.PlayerFaction.Id, request.Attacker.FactionId);
            Assert.AreEqual(WorldIds.None, request.Attacker.ArmyId);
            Assert.AreEqual(7, request.Attacker.CellId);
            Assert.AreEqual(7, request.Attacker.TargetCellId);
            Assert.AreEqual(2, request.Attacker.Squads.Length);
            Assert.AreEqual(1001, request.Attacker.Squads[0].UnitConfigId);
            Assert.AreEqual(3, request.Attacker.Squads[0].Count);

            Assert.IsFalse(request.Defender.IsPlayerParty);
            Assert.AreEqual(42, request.Defender.ArmyId);
            Assert.AreEqual(configSet.EnemyFaction.Id, request.Defender.FactionId);
            Assert.AreEqual(7, request.Defender.CellId);
            Assert.AreEqual(9, request.Defender.TargetCellId);
            Assert.AreEqual(1, request.Defender.Squads.Length);

            saveModel.World.Player.Party[0].Count = 99;
            saveModel.World.Armies[0].Squads[0].Count = 99;
            Assert.AreEqual(3, request.Attacker.Squads[0].Count, "Battle request must clone player party data.");
            Assert.AreEqual(4, request.Defender.Squads[0].Count, "Battle request must clone army squad data.");
        }

        [Test]
        public void CreatePlayerVsArmyCanPutPlayerOnDefenderSide()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel();
            var factory = new BattleEncounterRequestFactory(configSet.Database);

            var request = factory.CreatePlayerVsArmy(saveModel, opponentArmyId: 42, playerIsAttacker: false);

            Assert.AreEqual(42, request.Attacker.ArmyId);
            Assert.IsTrue(request.Defender.IsPlayerParty);
            Assert.AreEqual(configSet.PlayerFaction.Id, request.Defender.FactionId);
        }

        [Test]
        public void CreateArmyVsArmyBuildsRequestFromMatchingCellArmies()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel();
            var factory = new BattleEncounterRequestFactory(configSet.Database);

            var request = factory.CreateArmyVsArmy(saveModel, attackerArmyId: 42, defenderArmyId: 43);

            Assert.AreEqual(7, request.SourceCellId);
            Assert.AreEqual(42, request.Attacker.ArmyId);
            Assert.AreEqual(43, request.Defender.ArmyId);
            Assert.AreEqual(9, request.Attacker.TargetCellId);
            Assert.AreEqual(8, request.Defender.TargetCellId);
        }

        [Test]
        public void CreateBattleRequestRejectsUnknownOrSeparatedArmies()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel();
            var factory = new BattleEncounterRequestFactory(configSet.Database);

            Assert.Throws<InvalidOperationException>(() => factory.CreatePlayerVsArmy(saveModel, opponentArmyId: 999));

            saveModel.World.Armies[1].CellId = 8;
            Assert.Throws<InvalidOperationException>(() => factory.CreateArmyVsArmy(saveModel, attackerArmyId: 42, defenderArmyId: 43));
        }

        private static SaveModel CreateSaveModel()
        {
            return new SaveModel
            {
                World = new WorldModel
                {
                    Seed = 77,
                    Cells = new[]
                    {
                        new WorldCell { Id = 7 },
                        new WorldCell { Id = 8 }
                    },
                    Player = new PlayerGlobalData
                    {
                        FactionId = 0,
                        CellId = 7,
                        Party = new[]
                        {
                            new SquadData { UnitConfigId = 1001, Count = 3 },
                            new SquadData { UnitConfigId = 1002, Count = 1 }
                        }
                    },
                    Armies = new[]
                    {
                        new ArmyData
                        {
                            Id = 42,
                            FactionId = 2,
                            CellId = 7,
                            TargetCellId = 9,
                            Squads = new[]
                            {
                                new SquadData { UnitConfigId = 2001, Count = 4 }
                            }
                        },
                        new ArmyData
                        {
                            Id = 43,
                            FactionId = 1,
                            CellId = 7,
                            TargetCellId = 8,
                            Squads = new[]
                            {
                                new SquadData { UnitConfigId = 1001, Count = 2 }
                            }
                        }
                    }
                }
            };
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

        private sealed class TestConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                PlayerFaction = CreateConfig<FactionConfig>(1, "Player Faction");
                EnemyFaction = CreateConfig<FactionConfig>(2, "Enemy Faction");
                PlayerUnit = CreateConfig<UnitConfig>(1001, "Player Unit");
                var simulation = CreateConfig<BattleSimulationConfig>(10, "Battle Simulation");

                SetField(PlayerUnit, "factionId", PlayerFaction.Id);
                SetField(simulation, "playerUnit", PlayerUnit);

                SetField(Database, "factions", new[] { PlayerFaction, EnemyFaction });
                SetField(Database, "units", new[] { PlayerUnit });
                SetField(Database, "battleSimulation", simulation);
            }

            public ConfigDatabase Database { get; }
            public FactionConfig PlayerFaction { get; }
            public FactionConfig EnemyFaction { get; }
            public UnitConfig PlayerUnit { get; }

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

            private T Create<T>()
                where T : ScriptableObject
            {
                var asset = ScriptableObject.CreateInstance<T>();
                assets.Add(asset);
                return asset;
            }

            private T CreateConfig<T>(int id, string displayName)
                where T : IdentifiedConfig
            {
                var config = Create<T>();
                SetField(config, "id", id);
                SetField(config, "displayName", displayName);
                return config;
            }
        }
    }
}
