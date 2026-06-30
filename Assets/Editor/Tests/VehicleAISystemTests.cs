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
    public sealed class VehicleAISystemTests
    {
        [Test]
        public void AIControlledVehicleTargetsNearestOpponentAndRequestsAttack()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                var vehicle = CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    float2.zero,
                    cooldown: 0f,
                    range: 6f);
                CreateTarget(world, new float2(2f, 0f), BattleTeamType.Attacker);
                var farDefender = CreateTarget(world, new float2(5f, 0f), BattleTeamType.Defender);
                var nearestDefender = CreateTarget(world, new float2(4f, 0f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);
                world.Commit();

                Assert.AreEqual(nearestDefender, world.GetStash<TargetComponent>().Get(vehicle).Target);
                Assert.AreNotEqual(farDefender, world.GetStash<TargetComponent>().Get(vehicle).Target);
                var velocity = world.GetStash<VelocityComponent>().Get(vehicle).Value;
                Assert.AreEqual(0f, velocity.x, 0.001f);
                Assert.AreEqual(0f, velocity.y, 0.001f);

                var requests = CollectAttackRequests(world);
                Assert.AreEqual(1, requests.Count);
                Assert.AreEqual(vehicle, requests[0].Source);
                Assert.AreEqual(nearestDefender, requests[0].Target);
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ArtilleryVehicleTargetsClusterOverNearestSingleOpponent()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                var vehicle = CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    float2.zero,
                    cooldown: 0f,
                    range: 8f,
                    clusterWeapon: true);
                CreateTarget(world, new float2(2f, 0f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, 0f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, 0.5f), BattleTeamType.Defender);
                CreateTarget(world, new float2(5f, -0.5f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);
                world.Commit();

                var requests = CollectAttackRequests(world);
                Assert.AreEqual(1, requests.Count);
                Assert.AreEqual(vehicle, requests[0].Source);
                Assert.IsTrue(world.GetStash<ArtilleryTargetMarkerComponent>().Has(requests[0].Target));

                var targetPosition = world.GetStash<PositionComponent>().Get(requests[0].Target).Value;
                Assert.AreEqual(5f, targetPosition.x, 0.001f);
                Assert.AreEqual(0f, targetPosition.y, 0.001f);
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void AIControlledVehicleMovesTowardTargetOutsidePreferredRange()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                var vehicle = CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    float2.zero,
                    cooldown: 0f,
                    range: 6f);
                CreateTarget(world, new float2(5.5f, 0f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(vehicle).Value;
                Assert.AreEqual(1f, velocity.x, 0.001f);
                Assert.AreEqual(0f, velocity.y, 0.001f);
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void AIControlledVehicleUsesVehicleFlowFieldAroundBlockedTile()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                var vehicle = CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    new float2(0.5f, 1.5f),
                    cooldown: 1f,
                    range: 3.5f);
                CreateTarget(world, new float2(4.5f, 1.5f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world, CreateVehicleMapWithBlockedCenter());
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(vehicle).Value;
                Assert.Less(math.abs(velocity.x), 0.01f);
                Assert.Greater(math.abs(velocity.y), 0.99f);
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VehicleAIDoesNotFireWhenCooldownActiveOrVehicleNotAIControlled()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    float2.zero,
                    cooldown: 1f,
                    range: 6f);
                CreateVehicle(
                    world,
                    VehicleStateType.Empty,
                    BattleTeamType.Attacker,
                    new float2(0f, 1f),
                    cooldown: 0f,
                    range: 6f);
                CreateVehicle(
                    world,
                    VehicleStateType.PlayerControlled,
                    BattleTeamType.Attacker,
                    new float2(0f, 2f),
                    cooldown: 0f,
                    range: 6f);
                CreateTarget(world, new float2(4f, 0f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);
                world.Commit();

                Assert.IsEmpty(CollectAttackRequests(world));
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VehicleAIRunsAtDecisionCadence()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var vehicleAI = new VehicleAISystem(spatialHash);

            try
            {
                CreateVehicle(
                    world,
                    VehicleStateType.AIControlled,
                    BattleTeamType.Attacker,
                    float2.zero,
                    cooldown: 0f,
                    range: 6f);
                CreateTarget(world, new float2(4f, 0f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                vehicleAI.Initialize(session);
                vehicleAI.Tick(0f);
                world.Commit();
                Assert.AreEqual(1, CollectAttackRequests(world).Count);

                vehicleAI.Tick(0.1f);
                world.Commit();
                Assert.AreEqual(1, CollectAttackRequests(world).Count);

                vehicleAI.Tick(0.1f);
                world.Commit();
                Assert.AreEqual(2, CollectAttackRequests(world).Count);
            }
            finally
            {
                vehicleAI.Dispose();
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, BattleModel model = null)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                model ?? new BattleModel
                {
                    Width = 32,
                    Height = 32
                },
                world);
        }

        private static BattleModel CreateVehicleMapWithBlockedCenter()
        {
            var width = 5;
            var height = 3;
            var tiles = new Battle.Tiles.BattleTile[width * height];
            for (var index = 0; index < tiles.Length; index++)
            {
                tiles[index] = new Battle.Tiles.BattleTile
                {
                    Walkable = true,
                    Surface = Battle.Tiles.BattleTileSurface.Ground,
                    MoveCost = 1,
                    AllowedMoveLayers = Battle.Tiles.MoveLayer.Infantry | Battle.Tiles.MoveLayer.Vehicle
                };
            }

            tiles[1 * width + 1] = new Battle.Tiles.BattleTile
            {
                Walkable = true,
                Surface = Battle.Tiles.BattleTileSurface.Ground,
                MoveCost = 1,
                AllowedMoveLayers = Battle.Tiles.MoveLayer.Infantry
            };

            return new BattleModel
            {
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static Entity CreateVehicle(
            World world,
            VehicleStateType state,
            BattleTeamType team,
            float2 position,
            float cooldown,
            float range,
            bool clusterWeapon = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
            {
                VehicleConfigId = 601,
                State = state
            });
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = float2.zero
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
            world.GetStash<WeaponStatsComponent>().Set(entity, new WeaponStatsComponent
            {
                WeaponConfigId = 701,
                Type = clusterWeapon ? WeaponType.ArtilleryCannon : WeaponType.TankCannon,
                Damage = 10,
                Range = range,
                Cooldown = 1f,
                ProjectileSpeed = 4f,
                IsProjectile = clusterWeapon,
                UsesParabolicTrajectory = clusterWeapon,
                ExplosionRadius = clusterWeapon ? 1f : 0f
            });
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = cooldown
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
