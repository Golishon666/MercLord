using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleScalabilityTests
    {
        private const int TeamSize = 500;

        [Test]
        public void CoreAiLoopHandlesFiveHundredVsFiveHundredEntities()
        {
            using var configSet = new TestConfigSet(cellSize: 4f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var targetSearch = new TargetSearchSystem(spatialHash);
            var decisionSystem = new DecisionSystem(spatialHash);
            var movementSystem = new MovementSystem();

            try
            {
                for (var index = 0; index < TeamSize; index++)
                {
                    CreateCombatant(world, ResolveFormationPosition(index, xOffset: 18f), BattleTeamType.Attacker);
                    CreateCombatant(world, ResolveFormationPosition(index, xOffset: 20f), BattleTeamType.Defender);
                }

                world.Commit();
                var session = CreateSession(world);
                spatialHash.Initialize(session);
                targetSearch.Initialize(session);
                decisionSystem.Initialize(session);
                movementSystem.Initialize(session);

                Assert.AreEqual(TeamSize * 2, spatialHash.IndexedEntityCount);

                targetSearch.Tick(0.1f);
                world.Commit();
                Assert.AreEqual(TeamSize * 2, CountComponents<TargetComponent>(world));

                decisionSystem.Tick(0f);
                world.Commit();
                Assert.AreEqual(TeamSize * 2, CountComponents<AttackRequestComponent>(world));

                movementSystem.Tick(0.016f);
            }
            finally
            {
                movementSystem.Dispose();
                decisionSystem.Dispose();
                targetSearch.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 128,
                    Height = 96,
                    Tiles = CreateTiles(128, 96)
                },
                world);
        }

        private static BattleTile[] CreateTiles(int width, int height)
        {
            var tiles = new BattleTile[width * height];
            for (var index = 0; index < tiles.Length; index++)
            {
                tiles[index] = new BattleTile
                {
                    Walkable = true,
                    Surface = BattleTileSurface.Ground,
                    MoveCost = 1,
                    Cover = CoverType.None,
                    AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle
                };
            }

            return tiles;
        }

        private static float2 ResolveFormationPosition(int index, float xOffset)
        {
            return new float2(
                xOffset + index % 50,
                18f + index / 50);
        }

        private static void CreateCombatant(World world, float2 position, BattleTeamType team)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent());
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 100,
                Max = 100
            });
            world.GetStash<MovementStatsComponent>().Set(entity, new MovementStatsComponent
            {
                MoveSpeed = 3f,
                RotationSpeed = 360f
            });
            world.GetStash<WeaponStatsComponent>().Set(entity, new WeaponStatsComponent
            {
                WeaponConfigId = 900,
                Type = WeaponType.AutomaticRifle,
                DamageType = DamageType.Ballistic,
                Damage = 5,
                Range = 6f,
                Cooldown = 1f
            });
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent());
            world.GetStash<AIStatsComponent>().Set(entity, new AIStatsComponent
            {
                AIConfigId = 900,
                Type = AIType.Ranged,
                ThinkInterval = 1f,
                TargetSearchRadius = 6f,
                PreferredAttackDistance = 3f,
                RetreatHealthPercent = 0f
            });
            world.GetStash<AIThinkTimerComponent>().Set(entity, new AIThinkTimerComponent
            {
                TimeUntilNextThink = 0f
            });
            world.GetStash<BotStateComponent>().Set(entity, new BotStateComponent
            {
                Value = BotStateType.Idle
            });
        }

        private static int CountComponents<T>(World world)
            where T : struct, IComponent
        {
            var filter = world.Filter
                .With<T>()
                .Build();
            var count = 0;
            foreach (var entity in filter)
            {
                count++;
            }

            filter.Dispose();
            return count;
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
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

            public TestConfigSet(float cellSize)
            {
                Database = Create<ConfigDatabase>();
                var simulation = Create<BattleSimulationConfig>();
                SetField(simulation, "spatialHashCellSize", cellSize);
                SetField(Database, "battleSimulation", simulation);
            }

            public ConfigDatabase Database { get; }

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
        }
    }
}
