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
    public sealed class BattleSpatialHashSystemTests
    {
        [Test]
        public void SpatialHashIndexesOneThousandLiveEntitiesAndQueriesNearbyOpponents()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var results = new List<Entity>();
            var nearbyDefender = default(Entity);

            try
            {
                for (var entityIndex = 0; entityIndex < 500; entityIndex++)
                {
                    CreateIndexedEntity(
                        world,
                        new float2(entityIndex % 50, entityIndex / 50),
                        BattleTeamType.Attacker);
                }

                for (var entityIndex = 0; entityIndex < 500; entityIndex++)
                {
                    var position = entityIndex == 0
                        ? new float2(2f, 0f)
                        : new float2(100f + entityIndex % 50, entityIndex / 50);
                    var entity = CreateIndexedEntity(world, position, BattleTeamType.Defender);
                    if (entityIndex == 0)
                    {
                        nearbyDefender = entity;
                    }
                }

                world.Commit();
                spatialHash.Initialize(CreateSession(world));

                Assert.AreEqual(1000, spatialHash.IndexedEntityCount);
                Assert.Greater(spatialHash.ActiveBucketCount, 1);

                spatialHash.GetOpponentsInRange(
                    float2.zero,
                    3.01f,
                    BattleTeamType.Attacker,
                    results);

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(nearbyDefender, results[0]);
            }
            finally
            {
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SpatialHashRebuildCadenceUpdatesMovedAndDeadEntities()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var results = new List<Entity>();

            try
            {
                CreateIndexedEntity(world, float2.zero, BattleTeamType.Attacker);
                var defender = CreateIndexedEntity(world, new float2(20f, 0f), BattleTeamType.Defender);
                world.Commit();

                spatialHash.Initialize(CreateSession(world));
                spatialHash.GetOpponentsInRange(float2.zero, 5f, BattleTeamType.Attacker, results);
                Assert.IsEmpty(results);

                ref var defenderPosition = ref world.GetStash<PositionComponent>().Get(defender);
                defenderPosition.Value = new float2(1f, 0f);
                spatialHash.Tick(0.01f);

                spatialHash.GetOpponentsInRange(float2.zero, 5f, BattleTeamType.Attacker, results);
                Assert.IsEmpty(results);

                spatialHash.Tick(0.04f);

                spatialHash.GetOpponentsInRange(float2.zero, 5f, BattleTeamType.Attacker, results);
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(defender, results[0]);

                world.GetStash<DeadComponent>().Set(defender, new DeadComponent());
                world.Commit();
                spatialHash.Tick(0.01f);

                spatialHash.GetOpponentsInRange(float2.zero, 5f, BattleTeamType.Attacker, results);
                Assert.IsEmpty(results);
                Assert.AreEqual(2, spatialHash.IndexedEntityCount);

                spatialHash.Tick(0.04f);

                Assert.AreEqual(1, spatialHash.IndexedEntityCount);
            }
            finally
            {
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SpatialHashSkipsDriverEntities()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var results = new List<Entity>();

            try
            {
                CreateIndexedEntity(world, float2.zero, BattleTeamType.Attacker);
                var driver = CreateIndexedEntity(world, new float2(1f, 0f), BattleTeamType.Defender);
                world.GetStash<DriverComponent>().Set(driver, new DriverComponent());
                world.Commit();

                spatialHash.Initialize(CreateSession(world));

                Assert.AreEqual(1, spatialHash.IndexedEntityCount);
                spatialHash.GetOpponentsInRange(float2.zero, 5f, BattleTeamType.Attacker, results);
                Assert.IsEmpty(results);
            }
            finally
            {
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SpatialHashBuildsDebugBucketSnapshot()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var buckets = new List<SpatialHashBucketDebugInfo>();

            try
            {
                CreateIndexedEntity(world, new float2(0.25f, 0.25f), BattleTeamType.Attacker);
                CreateIndexedEntity(world, new float2(1.75f, 1.75f), BattleTeamType.Defender);
                CreateIndexedEntity(world, new float2(4.25f, 0.25f), BattleTeamType.Defender);
                world.Commit();

                spatialHash.Initialize(CreateSession(world));
                spatialHash.GetDebugBuckets(buckets);

                Assert.AreEqual(2, buckets.Count);
                AssertBucket(buckets, 0, 0, count: 2, cellSize: 2f);
                AssertBucket(buckets, 2, 0, count: 1, cellSize: 2f);
            }
            finally
            {
                spatialHash.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void TargetSearchUsesSpatialHashAndChoosesNearestOpponent()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var targetSearch = new TargetSearchSystem(spatialHash);

            try
            {
                var actor = CreateSearchActor(world, float2.zero, BattleTeamType.Attacker, searchRadius: 5f);
                CreateIndexedEntity(world, new float2(0.5f, 0f), BattleTeamType.Attacker);
                var farDefender = CreateIndexedEntity(world, new float2(4f, 0f), BattleTeamType.Defender);
                var nearestDefender = CreateIndexedEntity(world, new float2(2f, 0f), BattleTeamType.Defender);
                CreateIndexedEntity(world, new float2(8f, 0f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                targetSearch.Initialize(session);

                targetSearch.Tick(0.1f);

                var targets = world.GetStash<TargetComponent>();
                Assert.IsTrue(targets.Has(actor));
                Assert.AreEqual(nearestDefender, targets.Get(actor).Target);
                Assert.AreNotEqual(farDefender, targets.Get(actor).Target);
            }
            finally
            {
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
                    Height = 96
                },
                world);
        }

        private static Entity CreateSearchActor(
            World world,
            float2 position,
            BattleTeamType team,
            float searchRadius)
        {
            var entity = CreateIndexedEntity(world, position, team);
            world.GetStash<AIStatsComponent>().Set(entity, new AIStatsComponent
            {
                Type = AIType.Passive,
                ThinkInterval = 1f,
                TargetSearchRadius = searchRadius,
                PreferredAttackDistance = 1f,
                RetreatHealthPercent = 0f
            });
            world.GetStash<AIThinkTimerComponent>().Set(entity, new AIThinkTimerComponent
            {
                TimeUntilNextThink = 0f
            });
            return entity;
        }

        private static Entity CreateIndexedEntity(
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

        private static void AssertBucket(
            List<SpatialHashBucketDebugInfo> buckets,
            int cellX,
            int cellY,
            int count,
            float cellSize)
        {
            for (var bucketIndex = 0; bucketIndex < buckets.Count; bucketIndex++)
            {
                var bucket = buckets[bucketIndex];
                if (bucket.CellX != cellX || bucket.CellY != cellY)
                {
                    continue;
                }

                Assert.AreEqual(count, bucket.EntityCount);
                Assert.AreEqual(cellSize, bucket.CellSize, 0.001f);
                Assert.AreEqual(cellX * cellSize, bucket.Min.x, 0.001f);
                Assert.AreEqual(cellY * cellSize, bucket.Min.y, 0.001f);
                Assert.AreEqual((cellX + 1) * cellSize, bucket.Max.x, 0.001f);
                Assert.AreEqual((cellY + 1) * cellSize, bucket.Max.y, 0.001f);
                return;
            }

            Assert.Fail($"Expected debug bucket {cellX},{cellY}.");
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
