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
    public sealed class BattleProjectileExplosionTests
    {
        [Test]
        public void ProjectileExplosionDamagesNearbyOpponentsOnly()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var projectileSystem = new ProjectileSystem(spatialHash);

            try
            {
                var source = CreateUnit(world, new float2(0f, 0f), BattleTeamType.Attacker);
                var primaryTarget = CreateUnit(world, new float2(1f, 0f), BattleTeamType.Defender);
                var nearbyTarget = CreateUnit(world, new float2(1.6f, 0f), BattleTeamType.Defender);
                var outsideTarget = CreateUnit(world, new float2(4f, 0f), BattleTeamType.Defender);
                var friendlyTarget = CreateUnit(world, new float2(1f, 0.5f), BattleTeamType.Attacker);
                CreateProjectile(world, source, primaryTarget, new float2(0f, 0f), explosionRadius: 2f);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                projectileSystem.Initialize(session);
                projectileSystem.Tick(1f);
                world.Commit();

                var targets = CollectDamageRequestTargets(world);
                Assert.Contains(primaryTarget, targets);
                Assert.Contains(nearbyTarget, targets);
                Assert.IsFalse(targets.Contains(outsideTarget));
                Assert.IsFalse(targets.Contains(friendlyTarget));
                Assert.AreEqual(2, targets.Count);

                var shakes = CollectCameraShakes(world);
                Assert.AreEqual(1, shakes.Count);
                Assert.AreEqual(1f, shakes[0].Position.x, 0.001f);
                Assert.AreEqual(0f, shakes[0].Position.y, 0.001f);
                Assert.Greater(shakes[0].Shake.Intensity, 0f);
                Assert.AreEqual(shakes[0].Shake.Duration, shakes[0].Shake.RemainingTime, 0.001f);
            }
            finally
            {
                projectileSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ProjectileWithoutExplosionDamagesOnlyImpactTarget()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var projectileSystem = new ProjectileSystem(spatialHash);

            try
            {
                var source = CreateUnit(world, new float2(0f, 0f), BattleTeamType.Attacker);
                var primaryTarget = CreateUnit(world, new float2(1f, 0f), BattleTeamType.Defender);
                CreateUnit(world, new float2(1.5f, 0f), BattleTeamType.Defender);
                CreateProjectile(world, source, primaryTarget, new float2(0f, 0f));
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                projectileSystem.Initialize(session);
                projectileSystem.Tick(1f);
                world.Commit();

                var targets = CollectDamageRequestTargets(world);
                Assert.AreEqual(1, targets.Count);
                Assert.AreEqual(primaryTarget, targets[0]);
                Assert.IsEmpty(CollectCameraShakes(world));
            }
            finally
            {
                projectileSystem.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ParabolicProjectileExplosionDamagesNearbyOpponentsOnLanding()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var parabolicSystem = new ParabolicProjectileSystem(spatialHash);

            try
            {
                var source = CreateUnit(world, new float2(0f, 0f), BattleTeamType.Attacker);
                var primaryTarget = CreateUnit(world, new float2(3f, 0f), BattleTeamType.Defender);
                var nearbyTarget = CreateUnit(world, new float2(3f, 1f), BattleTeamType.Defender);
                CreateUnit(world, new float2(6f, 0f), BattleTeamType.Defender);
                CreateParabolicProjectile(world, source, primaryTarget, explosionRadius: 1.5f);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                parabolicSystem.Initialize(session);
                parabolicSystem.Tick(1f);
                world.Commit();

                var targets = CollectDamageRequestTargets(world);
                Assert.Contains(primaryTarget, targets);
                Assert.Contains(nearbyTarget, targets);
                Assert.AreEqual(2, targets.Count);

                var shakes = CollectCameraShakes(world);
                Assert.AreEqual(1, shakes.Count);
                Assert.AreEqual(3f, shakes[0].Position.x, 0.001f);
                Assert.AreEqual(0f, shakes[0].Position.y, 0.001f);
            }
            finally
            {
                parabolicSystem.Dispose();
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

        private static Entity CreateUnit(
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
            return entity;
        }

        private static Entity CreateProjectile(
            World world,
            Entity source,
            Entity target,
            float2 position,
            float explosionRadius = 0f)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<TargetComponent>().Set(entity, new TargetComponent
            {
                Target = target
            });
            world.GetStash<ProjectileComponent>().Set(entity, new ProjectileComponent
            {
                Source = source,
                Damage = 12,
                DamageType = DamageType.Explosion,
                Speed = 8f
            });

            if (explosionRadius > 0f)
            {
                world.GetStash<ExplosionOnImpactComponent>().Set(entity, new ExplosionOnImpactComponent
                {
                    Radius = explosionRadius
                });
            }

            return entity;
        }

        private static Entity CreateParabolicProjectile(
            World world,
            Entity source,
            Entity target,
            float explosionRadius)
        {
            var entity = CreateProjectile(world, source, target, new float2(0f, 0f), explosionRadius);
            world.GetStash<ParabolicProjectileComponent>().Set(entity, new ParabolicProjectileComponent
            {
                Start = new float2(0f, 0f),
                Target = new float2(3f, 0f),
                FlightTime = 1f,
                ElapsedTime = 0f,
                ArcHeight = 1f
            });
            return entity;
        }

        private static List<Entity> CollectDamageRequestTargets(World world)
        {
            var filter = world.Filter
                .With<DamageRequestComponent>()
                .Build();
            var damageRequests = world.GetStash<DamageRequestComponent>();
            var targets = new List<Entity>();
            foreach (var entity in filter)
            {
                targets.Add(damageRequests.Get(entity).Target);
            }

            filter.Dispose();
            return targets;
        }

        private static List<CameraShakeSnapshot> CollectCameraShakes(World world)
        {
            var filter = world.Filter
                .With<BattleCameraShakeComponent>()
                .Build();
            var shakes = world.GetStash<BattleCameraShakeComponent>();
            var result = new List<CameraShakeSnapshot>();
            foreach (var entity in filter)
            {
                var shake = shakes.Get(entity);
                result.Add(new CameraShakeSnapshot(shake.Position, shake));
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

        private readonly struct CameraShakeSnapshot
        {
            public CameraShakeSnapshot(float2 position, BattleCameraShakeComponent shake)
            {
                Position = position;
                Shake = shake;
            }

            public float2 Position { get; }
            public BattleCameraShakeComponent Shake { get; }
        }

        private sealed class TestConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                var simulation = Create<BattleSimulationConfig>();
                SetField(simulation, "spatialHashCellSize", 2f);
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
