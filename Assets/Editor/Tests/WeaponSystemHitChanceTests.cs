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
    public sealed class WeaponSystemHitChanceTests
    {
        [Test]
        public void HitscanWeaponDamagesTargetWhenHitChanceIsGuaranteed()
        {
            using var configSet = new TestConfigSet(new HitChanceFormula
            {
                BaseChance = 1f,
                MinimumChance = 1f
            });
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(2.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, range: 4f);
                CreateAttackRequest(world, source, target, weaponConfigId: 501);
                world.Commit();

                var session = CreateSession(CreateModel(width: 4, targetCover: CoverType.None), world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                var damageTargets = CollectDamageRequestTargets(world);
                Assert.AreEqual(1, damageTargets.Count);
                Assert.AreEqual(target, damageTargets[0]);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void HitscanRangePenaltyCanForceMissAtMaxRange()
        {
            using var configSet = new TestConfigSet(new HitChanceFormula
            {
                BaseChance = 1f,
                MinimumChance = 0f,
                RangePenaltyAtMaxRange = 1f
            });
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(4.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, range: 4f);
                CreateAttackRequest(world, source, target, weaponConfigId: 501);
                world.Commit();

                var session = CreateSession(CreateModel(width: 5, targetCover: CoverType.None), world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.IsEmpty(CollectDamageRequestTargets(world));
                Assert.AreEqual(1f, world.GetStash<AttackCooldownComponent>().Get(source).Value, 0.001f);
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void HitscanCoverPenaltyCanForceMiss()
        {
            using var configSet = new TestConfigSet(new HitChanceFormula
            {
                BaseChance = 1f,
                MinimumChance = 0f,
                MediumCoverPenalty = 1f
            });
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(2.5f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, range: 4f);
                CreateAttackRequest(world, source, target, weaponConfigId: 501);
                world.Commit();

                var session = CreateSession(CreateModel(width: 4, targetCover: CoverType.Medium), world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.IsEmpty(CollectDamageRequestTargets(world));
            }
            finally
            {
                weaponSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void HitscanMovingTargetPenaltyCanForceMiss()
        {
            using var configSet = new TestConfigSet(new HitChanceFormula
            {
                BaseChance = 1f,
                MinimumChance = 0f,
                MovingTargetPenalty = 1f
            });
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                var target = CreateCombatEntity(
                    world,
                    new float2(2.5f, 0.5f),
                    BattleTeamType.Defender,
                    velocity: new float2(1f, 0f));
                SetWeapon(world, source, range: 4f);
                CreateAttackRequest(world, source, target, weaponConfigId: 501);
                world.Commit();

                var session = CreateSession(CreateModel(width: 4, targetCover: CoverType.None), world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                Assert.IsEmpty(CollectDamageRequestTargets(world));
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
            return new BattleSession(
                new BattleGenerationRequest { Seed = 77 },
                model,
                world);
        }

        private static BattleModel CreateModel(int width, CoverType targetCover)
        {
            var tiles = new BattleTile[width];
            for (var index = 0; index < tiles.Length; index++)
            {
                tiles[index] = new BattleTile
                {
                    Walkable = true,
                    Surface = BattleTileSurface.Ground,
                    MoveCost = 1,
                    Cover = index == width - 2 ? targetCover : CoverType.None,
                    AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle,
                    BlocksLineOfSight = false,
                    BlocksProjectiles = false
                };
            }

            return new BattleModel
            {
                Width = width,
                Height = 1,
                Tiles = tiles
            };
        }

        private static Entity CreateCombatEntity(
            World world,
            float2 position,
            BattleTeamType team,
            float2 velocity = default)
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

            if (math.lengthsq(velocity) > 0f)
            {
                world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
                {
                    Value = velocity
                });
            }

            return entity;
        }

        private static void SetWeapon(World world, Entity source, float range)
        {
            world.GetStash<WeaponStatsComponent>().Set(source, new WeaponStatsComponent
            {
                WeaponConfigId = 501,
                Type = WeaponType.AutomaticRifle,
                DamageType = DamageType.Ballistic,
                Damage = 12,
                Range = range,
                Cooldown = 1f,
                IsProjectile = false
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

            public TestConfigSet(HitChanceFormula hitChanceFormula)
            {
                Database = Create<ConfigDatabase>();
                var simulation = Create<BattleSimulationConfig>();
                var combatBalance = Create<CombatBalanceConfig>();
                SetField(simulation, "spatialHashCellSize", 2f);
                SetField(combatBalance, "damageFormula", new DamageFormula { MinimumDamage = 1 });
                SetField(combatBalance, "hitChanceFormula", hitChanceFormula);
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
