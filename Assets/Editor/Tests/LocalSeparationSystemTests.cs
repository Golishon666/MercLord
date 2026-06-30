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
    public sealed class LocalSeparationSystemTests
    {
        [Test]
        public void LocalSeparationPushesNearbyMoversApart()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var separation = new LocalSeparationSystem(spatialHash);

            try
            {
                var left = CreateMover(world, float2.zero, BattleTeamType.Attacker);
                var right = CreateMover(world, new float2(0.2f, 0f), BattleTeamType.Attacker);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                separation.Initialize(session);
                separation.Tick(0f);

                var velocities = world.GetStash<VelocityComponent>();
                var leftVelocity = velocities.Get(left).Value;
                var rightVelocity = velocities.Get(right).Value;
                Assert.Less(leftVelocity.x, -0.1f);
                Assert.Greater(rightVelocity.x, 0.1f);
                Assert.AreEqual(0f, leftVelocity.y, 0.001f);
                Assert.AreEqual(0f, rightVelocity.y, 0.001f);
            }
            finally
            {
                separation.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LocalSeparationBlendsWithExistingVelocityAndClamps()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var separation = new LocalSeparationSystem(spatialHash);

            try
            {
                var mover = CreateMover(world, float2.zero, BattleTeamType.Attacker, velocity: new float2(1f, 0f));
                CreateOccupant(world, new float2(0f, 0.2f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                separation.Initialize(session);
                separation.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(mover).Value;
                Assert.Greater(velocity.x, 0.9f);
                Assert.Less(velocity.y, -0.1f);
                Assert.LessOrEqual(math.length(velocity), 1.001f);
            }
            finally
            {
                separation.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LocalSeparationDoesNotChangePlayerControlledVelocity()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var separation = new LocalSeparationSystem(spatialHash);

            try
            {
                var player = CreateMover(
                    world,
                    float2.zero,
                    BattleTeamType.Attacker,
                    velocity: float2.zero,
                    playerControlled: true);
                var squadMate = CreateMover(world, new float2(0.2f, 0f), BattleTeamType.Attacker);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                separation.Initialize(session);
                separation.Tick(0f);

                var velocities = world.GetStash<VelocityComponent>();
                var playerVelocity = velocities.Get(player).Value;
                var squadMateVelocity = velocities.Get(squadMate).Value;
                Assert.AreEqual(0f, playerVelocity.x, 0.001f);
                Assert.AreEqual(0f, playerVelocity.y, 0.001f);
                Assert.Greater(squadMateVelocity.x, 0.1f);
            }
            finally
            {
                separation.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LocalSeparationIgnoresDeadOccupants()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var separation = new LocalSeparationSystem(spatialHash);

            try
            {
                var mover = CreateMover(world, float2.zero, BattleTeamType.Attacker);
                CreateOccupant(world, new float2(0.2f, 0f), BattleTeamType.Defender, dead: true);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                separation.Initialize(session);
                separation.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(mover).Value;
                Assert.AreEqual(0f, velocity.x, 0.001f);
                Assert.AreEqual(0f, velocity.y, 0.001f);
            }
            finally
            {
                separation.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LocalSeparationRunsAtFixedCadence()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var separation = new LocalSeparationSystem(spatialHash);

            try
            {
                var left = CreateMover(world, float2.zero, BattleTeamType.Attacker);
                CreateMover(world, new float2(0.2f, 0f), BattleTeamType.Attacker);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                separation.Initialize(session);
                separation.Tick(0f);

                ref var leftVelocity = ref world.GetStash<VelocityComponent>().Get(left);
                Assert.Less(leftVelocity.Value.x, -0.1f);
                leftVelocity.Value = float2.zero;

                separation.Tick(0.01f);
                Assert.AreEqual(0f, leftVelocity.Value.x, 0.001f);
                Assert.AreEqual(0f, leftVelocity.Value.y, 0.001f);

                separation.Tick(0.04f);
                Assert.Less(leftVelocity.Value.x, -0.1f);
            }
            finally
            {
                separation.Dispose();
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

        private static Entity CreateMover(
            World world,
            float2 position,
            BattleTeamType team,
            float2 velocity = default,
            bool playerControlled = false)
        {
            var entity = CreateOccupant(world, position, team);
            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = velocity
            });
            world.GetStash<MovementStatsComponent>().Set(entity, new MovementStatsComponent
            {
                MoveSpeed = 1f,
                RotationSpeed = 1f
            });

            if (playerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            }

            return entity;
        }

        private static Entity CreateOccupant(
            World world,
            float2 position,
            BattleTeamType team,
            bool dead = false)
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

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(entity, new DeadComponent());
            }

            return entity;
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
