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
    public sealed class WeaponSystemMeleeTests
    {
        [Test]
        public void MeleeWeaponDamagesNearbyOpponentsOnly()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, float2.zero, BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(0.8f, 0f), BattleTeamType.Defender);
                var nearbyOpponent = CreateCombatEntity(world, new float2(0.4f, 0.5f), BattleTeamType.Defender);
                var outsideOpponent = CreateCombatEntity(world, new float2(2f, 0f), BattleTeamType.Defender);
                var friendly = CreateCombatEntity(world, new float2(0.2f, 0f), BattleTeamType.Attacker);
                SetWeapon(world, source, WeaponType.Sword, range: 1f);
                CreateAttackRequest(world, source, target, weaponConfigId: 301);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                var damageTargets = CollectDamageRequestTargets(world);
                Assert.Contains(target, damageTargets);
                Assert.Contains(nearbyOpponent, damageTargets);
                Assert.IsFalse(damageTargets.Contains(outsideOpponent));
                Assert.IsFalse(damageTargets.Contains(friendly));
                Assert.AreEqual(2, damageTargets.Count);
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
        public void RangedHitscanDamagesOnlyRequestedTarget()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var weaponSystem = new WeaponSystem(spatialHash, configSet.Database);

            try
            {
                var source = CreateCombatEntity(world, float2.zero, BattleTeamType.Attacker);
                var target = CreateCombatEntity(world, new float2(0.8f, 0f), BattleTeamType.Defender);
                var nearbyOpponent = CreateCombatEntity(world, new float2(0.4f, 0.5f), BattleTeamType.Defender);
                SetWeapon(world, source, WeaponType.AutomaticRifle, range: 1f);
                CreateAttackRequest(world, source, target, weaponConfigId: 301);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                weaponSystem.Initialize(session);
                weaponSystem.Tick(0f);
                world.Commit();

                var damageTargets = CollectDamageRequestTargets(world);
                Assert.AreEqual(1, damageTargets.Count);
                Assert.AreEqual(target, damageTargets[0]);
                Assert.IsFalse(damageTargets.Contains(nearbyOpponent));

                var traces = CollectHitscanTraces(world);
                Assert.AreEqual(1, traces.Count);
                Assert.AreEqual(float2.zero, traces[0].Start);
                Assert.AreEqual(new float2(0.8f, 0f), traces[0].End);
                Assert.IsTrue(traces[0].Hit);
                Assert.Greater(traces[0].RemainingTime, 0f);

                var audioCues = CollectAudioCues(world);
                Assert.AreEqual(1, audioCues.Count);
                Assert.AreEqual(BattleAudioCueType.HitscanShot, audioCues[0].Type);
                Assert.AreEqual(float2.zero, audioCues[0].Position);
            }
            finally
            {
                weaponSystem.Dispose();
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

        private static Entity CreateCombatEntity(
            World world,
            float2 position,
            BattleTeamType team)
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
            float range)
        {
            world.GetStash<WeaponStatsComponent>().Set(source, new WeaponStatsComponent
            {
                WeaponConfigId = 301,
                Type = weaponType,
                DamageType = DamageType.Ballistic,
                Damage = 12,
                Range = range,
                Cooldown = 1f,
                IsProjectile = false
            });
            world.GetStash<AttackCooldownComponent>().Set(source, new AttackCooldownComponent
            {
                Value = 0f
            });
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

        private static List<BattleAudioCueComponent> CollectAudioCues(World world)
        {
            var filter = world.Filter
                .With<BattleAudioCueComponent>()
                .Build();
            var cues = world.GetStash<BattleAudioCueComponent>();
            var result = new List<BattleAudioCueComponent>();
            foreach (var entity in filter)
            {
                result.Add(cues.Get(entity));
            }

            filter.Dispose();
            return result;
        }

        private static List<HitscanTraceComponent> CollectHitscanTraces(World world)
        {
            var filter = world.Filter
                .With<HitscanTraceComponent>()
                .Build();
            var traces = world.GetStash<HitscanTraceComponent>();
            var result = new List<HitscanTraceComponent>();
            foreach (var entity in filter)
            {
                result.Add(traces.Get(entity));
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
