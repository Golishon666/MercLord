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
    public sealed class WeaponSystemLineOfFireTests
    {
        [Test]
        public void HitscanWeaponDoesNotDamageThroughBlockingTile()
        {
            using var configSet = new TestConfigSet();
            var model = CreateModel();
            SetBlockingTile(model, x: 1, y: 0);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(2.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, WeaponType.AutomaticRifle, isProjectile: false, usesParabolic: false);
                CreateAttackRequest(world, source, target, weaponConfigId: 401);
                world.Commit();

                var session = CreateSession(model, world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.IsEmpty(CollectDamageRequestTargets(world));
                Assert.AreEqual(0f, world.GetStash<AttackCooldownComponent>().Get(source).Value, 0.001f);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void DirectProjectileDoesNotSpawnThroughProjectileBlockingTile()
        {
            using var configSet = new TestConfigSet();
            var model = CreateModel();
            SetBlockingTile(model, x: 1, y: 0);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(2.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, WeaponType.TankCannon, isProjectile: true, usesParabolic: false);
                CreateAttackRequest(world, source, target, weaponConfigId: 401);
                world.Commit();

                var session = CreateSession(model, world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.AreEqual(0, CountProjectiles(world));
                Assert.AreEqual(0f, world.GetStash<AttackCooldownComponent>().Get(source).Value, 0.001f);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ParabolicProjectileIgnoresBlockingTile()
        {
            using var configSet = new TestConfigSet();
            var model = CreateModel();
            SetBlockingTile(model, x: 1, y: 0);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(2.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, WeaponType.ArtilleryCannon, isProjectile: true, usesParabolic: true);
                CreateAttackRequest(world, source, target, weaponConfigId: 401);
                world.Commit();

                var session = CreateSession(model, world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.AreEqual(1, CountProjectiles(world));
                Assert.AreEqual(1f, world.GetStash<AttackCooldownComponent>().Get(source).Value, 0.001f);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(BattleModel model, World world)
        {
            return new BattleSession(new BattleGenerationRequest(), model, world);
        }

        private static BattleModel CreateModel()
        {
            var tiles = new BattleTile[3];
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

            return new BattleModel
            {
                Width = 3,
                Height = 1,
                Tiles = tiles
            };
        }

        private static void SetBlockingTile(BattleModel model, int x, int y)
        {
            model.Tiles[y * model.Width + x] = new BattleTile
            {
                Walkable = false,
                Surface = BattleTileSurface.Obstacle,
                MoveCost = 1,
                Cover = CoverType.Heavy,
                AllowedMoveLayers = MoveLayer.None,
                BlocksLineOfSight = true,
                BlocksProjectiles = true
            };
        }

        private static Entity CreateCombatEntity(World world, float2 position, BattleTeamType team)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 100,
                Max = 100
            });
            return entity;
        }

        private static void SetWeapon(
            World world,
            Entity source,
            WeaponType weaponType,
            bool isProjectile,
            bool usesParabolic)
        {
            world.GetStash<WeaponStatsComponent>().Set(source, new WeaponStatsComponent
            {
                WeaponConfigId = 401,
                Type = weaponType,
                DamageType = isProjectile ? DamageType.Explosion : DamageType.Ballistic,
                Damage = 20,
                Range = 5f,
                Cooldown = 1f,
                ProjectileSpeed = 4f,
                IsProjectile = isProjectile,
                UsesParabolicTrajectory = usesParabolic,
                ParabolicArcHeight = usesParabolic ? 1f : 0f,
                ExplosionRadius = usesParabolic ? 1f : 0f
            });
            world.GetStash<AttackCooldownComponent>().Set(source, new AttackCooldownComponent());
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

        private static List<Entity> CollectDamageRequestTargets(World world)
        {
            var filter = world.Filter
                .With<DamageRequestComponent>()
                .Build();
            var damageRequests = world.GetStash<DamageRequestComponent>();
            var result = new List<Entity>();
            foreach (var entity in filter)
            {
                result.Add(damageRequests.Get(entity).Target);
            }

            filter.Dispose();
            return result;
        }

        private static int CountProjectiles(World world)
        {
            var filter = world.Filter
                .With<ProjectileComponent>()
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

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                var simulation = Create<BattleSimulationConfig>();
                var combatBalance = Create<CombatBalanceConfig>();
                SetField(simulation, "spatialHashCellSize", 2f);
                SetField(combatBalance, "damageFormula", new DamageFormula { MinimumDamage = 1 });
                SetField(combatBalance, "hitChanceFormula", new HitChanceFormula
                {
                    BaseChance = 1f,
                    MinimumChance = 1f
                });
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
