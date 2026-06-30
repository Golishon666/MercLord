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
    public sealed class ArtilleryWarningSystemTests
    {
        [Test]
        public void WeaponSystemSpawnsWarningForExplosiveParabolicProjectile()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0f, 0f));
                var target = CreateCombatEntity(world, new float2(4f, 0f));
                world.GetStash<WeaponStatsComponent>().Set(source, new WeaponStatsComponent
                {
                    WeaponConfigId = 701,
                    DamageType = DamageType.Explosion,
                    Damage = 30,
                    Range = 8f,
                    Cooldown = 1f,
                    IsProjectile = true,
                    UsesParabolicTrajectory = true,
                    ProjectileSpeed = 2f,
                    ParabolicArcHeight = 1f,
                    ExplosionRadius = 2.5f
                });
                world.GetStash<AttackCooldownComponent>().Set(source, new AttackCooldownComponent());
                CreateAttackRequest(world, source, target, weaponConfigId: 701);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                var warnings = CollectWarnings(world);
                Assert.AreEqual(1, warnings.Count);
                var warning = warnings[0].Warning;
                Assert.AreEqual(source, warning.Source);
                Assert.AreEqual(2.5f, warning.Radius, 0.001f);
                Assert.AreEqual(2f, warning.Duration, 0.001f);
                Assert.AreEqual(2f, warning.RemainingTime, 0.001f);
                Assert.AreEqual(4f, warnings[0].Position.x, 0.001f);
                Assert.AreEqual(0f, warnings[0].Position.y, 0.001f);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void WeaponSystemRemovesTemporaryArtilleryTargetMarkerAfterShot()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0f, 0f));
                var marker = CreateArtilleryTargetMarker(world, new float2(4f, 0f));
                world.GetStash<WeaponStatsComponent>().Set(source, new WeaponStatsComponent
                {
                    WeaponConfigId = 701,
                    DamageType = DamageType.Explosion,
                    Damage = 30,
                    Range = 8f,
                    Cooldown = 1f,
                    IsProjectile = true,
                    UsesParabolicTrajectory = true,
                    ProjectileSpeed = 2f,
                    ParabolicArcHeight = 1f,
                    ExplosionRadius = 2.5f
                });
                world.GetStash<AttackCooldownComponent>().Set(source, new AttackCooldownComponent());
                CreateAttackRequest(world, source, marker, weaponConfigId: 701);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.IsFalse(world.Has(marker));
                Assert.AreEqual(1, CollectWarnings(world).Count);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void WarningSystemRemovesExpiredWarningsOnly()
        {
            var world = World.Create();
            var warningSystem = new ArtilleryWarningSystem();

            try
            {
                var warning = CreateWarning(world, remainingTime: 0.25f);
                world.Commit();

                warningSystem.Initialize(CreateSession(world));
                warningSystem.Tick(0.1f);
                world.Commit();

                Assert.IsTrue(world.Has(warning));
                Assert.AreEqual(0.15f, world.GetStash<ArtilleryWarningComponent>().Get(warning).RemainingTime, 0.001f);

                warningSystem.Tick(0.2f);
                world.Commit();

                Assert.IsFalse(world.Has(warning));
            }
            finally
            {
                warningSystem.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 1,
                    Height = 1,
                    Tiles = new[]
                    {
                        new BattleTile
                        {
                            Walkable = true,
                            MoveCost = 1,
                            AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle
                        }
                    }
                },
                world);
        }

        private static Entity CreateCombatEntity(World world, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            return entity;
        }

        private static void CreateAttackRequest(World world, Entity source, Entity target, int weaponConfigId)
        {
            var entity = world.CreateEntity();
            world.GetStash<AttackRequestComponent>().Set(entity, new AttackRequestComponent
            {
                Source = source,
                Target = target,
                WeaponConfigId = weaponConfigId
            });
        }

        private static Entity CreateArtilleryTargetMarker(World world, float2 position)
        {
            var entity = CreateCombatEntity(world, position);
            world.GetStash<ArtilleryTargetMarkerComponent>().Set(entity, new ArtilleryTargetMarkerComponent());
            return entity;
        }

        private static Entity CreateWarning(World world, float remainingTime)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = float2.zero
            });
            world.GetStash<ArtilleryWarningComponent>().Set(entity, new ArtilleryWarningComponent
            {
                Radius = 2f,
                Duration = 0.25f,
                RemainingTime = remainingTime
            });
            return entity;
        }

        private static List<WarningSnapshot> CollectWarnings(World world)
        {
            var filter = world.Filter
                .With<PositionComponent>()
                .With<ArtilleryWarningComponent>()
                .Build();
            var positions = world.GetStash<PositionComponent>();
            var warnings = world.GetStash<ArtilleryWarningComponent>();
            var result = new List<WarningSnapshot>();
            foreach (var entity in filter)
            {
                result.Add(new WarningSnapshot(positions.Get(entity).Value, warnings.Get(entity)));
            }

            filter.Dispose();
            return result;
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

        private readonly struct WarningSnapshot
        {
            public WarningSnapshot(float2 position, ArtilleryWarningComponent warning)
            {
                Position = position;
                Warning = warning;
            }

            public float2 Position { get; }
            public ArtilleryWarningComponent Warning { get; }
        }

        private sealed class TestConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                var simulation = Create<BattleSimulationConfig>();
                var combatBalance = Create<CombatBalanceConfig>();
                SetField(simulation, "spatialHashCellSize", 2f);
                SetField(combatBalance, "damageFormula", new DamageFormula { MinimumDamage = 1 });
                SetField(combatBalance, "hitChanceFormula", HitChanceFormula.Default);
                SetField(Database, "battleSimulation", simulation);
                SetField(Database, "combatBalance", combatBalance);
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
