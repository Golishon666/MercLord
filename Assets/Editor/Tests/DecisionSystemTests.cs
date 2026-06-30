using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class DecisionSystemTests
    {
        [Test]
        public void ArtilleryAIPrefersClusterMarkerOverAssignedNearestTarget()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var decisionSystem = new DecisionSystem(spatialHash);

            try
            {
                var actor = CreateArtilleryActor(world, float2.zero, BattleTeamType.Attacker);
                var nearestTarget = CreateTarget(world, new float2(2f, 0f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, 0f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, 0.5f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, -0.5f), BattleTeamType.Defender);
                world.GetStash<TargetComponent>().Set(actor, new TargetComponent { Target = nearestTarget });
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                decisionSystem.Initialize(session);
                decisionSystem.Tick(0f);
                world.Commit();

                var requests = CollectAttackRequests(world);
                Assert.AreEqual(1, requests.Count);
                Assert.AreEqual(actor, requests[0].Source);
                Assert.AreNotEqual(nearestTarget, requests[0].Target);
                Assert.IsTrue(world.GetStash<ArtilleryTargetMarkerComponent>().Has(requests[0].Target));

                var targetPosition = world.GetStash<PositionComponent>().Get(requests[0].Target).Value;
                Assert.AreEqual(5f, targetPosition.x, 0.001f);
                Assert.AreEqual(0f, targetPosition.y, 0.001f);
            }
            finally
            {
                decisionSystem.Dispose();
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
                    Width = 16,
                    Height = 16
                },
                world);
        }

        private static Entity CreateArtilleryActor(World world, float2 position, BattleTeamType team)
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
            world.GetStash<WeaponStatsComponent>().Set(entity, new WeaponStatsComponent
            {
                WeaponConfigId = 701,
                Type = WeaponType.ArtilleryCannon,
                Damage = 20,
                Range = 8f,
                Cooldown = 1f,
                ProjectileSpeed = 4f,
                IsProjectile = true,
                UsesParabolicTrajectory = true,
                ExplosionRadius = 1f
            });
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent());
            world.GetStash<AIStatsComponent>().Set(entity, new AIStatsComponent
            {
                Type = AIType.Artillery,
                PreferredAttackDistance = 6f
            });
            return entity;
        }

        private static Entity CreateTarget(World world, float2 position, BattleTeamType team)
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
                Current = 10,
                Max = 10
            });
            return entity;
        }

        private static List<AttackRequestComponent> CollectAttackRequests(World world)
        {
            var filter = world.Filter
                .With<AttackRequestComponent>()
                .Build();
            var attacks = world.GetStash<AttackRequestComponent>();
            var result = new List<AttackRequestComponent>();
            foreach (var entity in filter)
            {
                result.Add(attacks.Get(entity));
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
