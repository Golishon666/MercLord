using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Game.StateMachine;
using MercLord.Global.Cells;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class GlobalBattleStarterTests
    {
        [Test]
        public async Task TryStartPlayerBattleInCellAsyncTransitionsToEnterBattle()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel(
                WorldIds.None,
                new ArmyData
                {
                    Id = 42,
                    FactionId = configSet.EnemyFaction.Id,
                    CellId = 7,
                    TargetCellId = 8,
                    Squads = new[] { new SquadData { UnitConfigId = 2001, Count = 3 } }
                });
            var stateMachine = new FakeGameStateMachine();
            var starter = CreateStarter(configSet.Database, saveModel, stateMachine);

            var started = await starter.TryStartPlayerBattleInCellAsync(7);

            Assert.IsTrue(started);
            Assert.IsTrue(starter.TransitionRequested);
            Assert.AreEqual(1, stateMachine.ChangeCount);
            Assert.AreEqual(GameStateId.EnterBattle, stateMachine.LastStateId);
            Assert.IsInstanceOf<EnterBattleRequest>(stateMachine.LastPayload);

            var payload = (EnterBattleRequest)stateMachine.LastPayload;
            Assert.AreEqual(7, payload.SourceCellId);
            Assert.IsTrue(payload.Attacker.IsPlayerParty);
            Assert.AreEqual(configSet.PlayerFaction.Id, payload.Attacker.FactionId);
            Assert.AreEqual(42, payload.Defender.ArmyId);
            Assert.AreEqual(configSet.EnemyFaction.Id, payload.Defender.FactionId);
        }

        [Test]
        public void TryGetPlayerBattleEncounterBuildsOpponentSummary()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel(
                WorldIds.None,
                new ArmyData
                {
                    Id = 42,
                    FactionId = configSet.EnemyFaction.Id,
                    CellId = 7,
                    TargetCellId = 8,
                    Squads = new[]
                    {
                        new SquadData { UnitConfigId = 2001, Count = 3 },
                        new SquadData { UnitConfigId = 2002, Count = 4 }
                    }
                });
            var starter = CreateStarter(configSet.Database, saveModel, new FakeGameStateMachine());

            var found = starter.TryGetPlayerBattleEncounter(7, out var encounter);

            Assert.IsTrue(found);
            Assert.AreEqual(7, encounter.CellId);
            Assert.AreEqual(42, encounter.OpponentArmyId);
            Assert.AreEqual(configSet.PlayerFaction.Id, encounter.PlayerFactionId);
            Assert.AreEqual(configSet.EnemyFaction.Id, encounter.OpponentFactionId);
            Assert.AreEqual(2, encounter.PlayerUnitCount);
            Assert.AreEqual(7, encounter.OpponentUnitCount);
        }

        [Test]
        public async Task TryStartPlayerBattleInCellAsyncIgnoresCellsWithoutPlayerBattle()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel(
                configSet.PlayerFaction.Id,
                new ArmyData
                {
                    Id = 42,
                    FactionId = configSet.EnemyFaction.Id,
                    CellId = 8,
                    Squads = new[] { new SquadData { UnitConfigId = 2001, Count = 1 } }
                });
            var stateMachine = new FakeGameStateMachine();
            var starter = CreateStarter(configSet.Database, saveModel, stateMachine);

            var started = await starter.TryStartPlayerBattleInCellAsync(8);

            Assert.IsFalse(started);
            Assert.IsFalse(starter.TransitionRequested);
            Assert.AreEqual(0, stateMachine.ChangeCount);
        }

        [Test]
        public async Task TryStartPlayerBattleInCellAsyncSkipsFriendlyArmy()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel(
                WorldIds.None,
                new ArmyData
                {
                    Id = 41,
                    FactionId = configSet.PlayerFaction.Id,
                    CellId = 7,
                    Squads = new[] { new SquadData { UnitConfigId = 1001, Count = 2 } }
                },
                new ArmyData
                {
                    Id = 42,
                    FactionId = configSet.EnemyFaction.Id,
                    CellId = 7,
                    Squads = new[] { new SquadData { UnitConfigId = 2001, Count = 2 } }
                });
            var stateMachine = new FakeGameStateMachine();
            var starter = CreateStarter(configSet.Database, saveModel, stateMachine);

            Assert.IsTrue(starter.TryFindPlayerBattleOpponent(7, out var opponentArmyId));
            Assert.AreEqual(42, opponentArmyId);

            var started = await starter.TryStartPlayerBattleInCellAsync(7);

            Assert.IsTrue(started);
            var payload = (EnterBattleRequest)stateMachine.LastPayload;
            Assert.AreEqual(42, payload.Defender.ArmyId);
        }

        [Test]
        public async Task TryStartPlayerBattleAsyncDoesNotTransitionTwice()
        {
            using var configSet = new TestConfigSet();
            var saveModel = CreateSaveModel(
                configSet.PlayerFaction.Id,
                new ArmyData
                {
                    Id = 42,
                    FactionId = configSet.EnemyFaction.Id,
                    CellId = 7,
                    Squads = new[] { new SquadData { UnitConfigId = 2001, Count = 1 } }
                });
            var stateMachine = new FakeGameStateMachine();
            var starter = CreateStarter(configSet.Database, saveModel, stateMachine);

            var firstStarted = await starter.TryStartPlayerBattleAsync(42);
            var secondStarted = await starter.TryStartPlayerBattleAsync(42);

            Assert.IsTrue(firstStarted);
            Assert.IsFalse(secondStarted);
            Assert.AreEqual(1, stateMachine.ChangeCount);
        }

        private static GlobalBattleStarter CreateStarter(
            ConfigDatabase configDatabase,
            SaveModel saveModel,
            IGameStateMachine stateMachine)
        {
            var saveService = new FakeSaveService(saveModel);
            return new GlobalBattleStarter(
                saveService,
                new BattleEncounterRequestFactory(configDatabase),
                stateMachine,
                configDatabase);
        }

        private static SaveModel CreateSaveModel(int playerFactionId, params ArmyData[] armies)
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
                        FactionId = playerFactionId,
                        CellId = 7,
                        Party = new[]
                        {
                            new SquadData { UnitConfigId = 1001, Count = 2 }
                        }
                    },
                    Armies = armies
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

        private sealed class FakeSaveService : ISaveService
        {
            public FakeSaveService(SaveModel saveModel)
            {
                Current = saveModel;
            }

            public SaveModel Current { get; private set; }

            public void SetCurrent(SaveModel saveModel)
            {
                Current = saveModel;
            }

            public SaveModel CreateNew(int seed)
            {
                Current = new SaveModel
                {
                    World = new WorldModel { Seed = seed }
                };
                return Current;
            }

            public SaveModel CreateNew(WorldModel world)
            {
                Current = new SaveModel { World = world };
                return Current;
            }
        }

        private sealed class FakeGameStateMachine : IGameStateMachine
        {
            public GameStateId? CurrentStateId { get; private set; }
            public int ChangeCount { get; private set; }
            public GameStateId LastStateId { get; private set; }
            public object LastPayload { get; private set; }

            public void Register(IGameState state)
            {
            }

            public UniTask ChangeStateAsync(
                GameStateId stateId,
                object payload = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ChangeCount++;
                LastStateId = stateId;
                LastPayload = payload;
                CurrentStateId = stateId;
                return UniTask.CompletedTask;
            }
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
