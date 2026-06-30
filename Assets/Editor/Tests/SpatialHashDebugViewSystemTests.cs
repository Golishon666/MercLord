using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class SpatialHashDebugViewSystemTests
    {
        [Test]
        public void SpatialHashDebugViewSystemRendersOnlySpatialBucketOverlayMode()
        {
            using var configSet = new TestConfigSet(cellSize: 2f);
            var world = World.Create();
            var spatialHash = new SpatialHashSystem(configSet.Database);
            var catalog = CreateCatalog();
            var tilemapObject = new GameObject("Tilemap View");
            var tilemapView = tilemapObject.AddComponent<BattleTilemapView>();
            var viewSystem = new SpatialHashDebugViewSystem(spatialHash, catalog, tilemapView);

            try
            {
                CreateIndexedEntity(world, new float2(0.5f, 0.5f), BattleTeamType.Attacker);
                CreateIndexedEntity(world, new float2(4.5f, 0.5f), BattleTeamType.Defender);
                world.Commit();

                var session = CreateSession(world);
                spatialHash.Initialize(session);
                viewSystem.Initialize(session);
                viewSystem.Tick(0f);

                var root = GameObject.Find("Battle Spatial Hash Debug Views");
                Assert.IsNotNull(root);
                Assert.IsEmpty(root.GetComponentsInChildren<LineRenderer>());

                SetField(tilemapView, "debugOverlayMode", BattleDebugOverlayMode.SpatialBuckets);
                viewSystem.Tick(0f);

                var renderers = root.GetComponentsInChildren<LineRenderer>();
                Assert.AreEqual(2, renderers.Length);
                var firstBucket = FindRenderer(renderers, "Spatial Bucket 0,0");
                Assert.IsNotNull(firstBucket);
                Assert.AreEqual(0f, firstBucket.GetPosition(0).x, 0.001f);
                Assert.AreEqual(0f, firstBucket.GetPosition(0).y, 0.001f);
                Assert.AreEqual(2f, firstBucket.GetPosition(1).x, 0.001f);
                Assert.AreEqual(0f, firstBucket.GetPosition(1).y, 0.001f);
                Assert.AreEqual(2f, firstBucket.GetPosition(2).x, 0.001f);
                Assert.AreEqual(2f, firstBucket.GetPosition(2).y, 0.001f);

                SetField(tilemapView, "debugOverlayMode", BattleDebugOverlayMode.None);
                viewSystem.Tick(0f);

                Assert.IsEmpty(root.GetComponentsInChildren<LineRenderer>());
            }
            finally
            {
                viewSystem.Dispose();
                spatialHash.Dispose();
                UnityEngine.Object.DestroyImmediate(tilemapObject);
                UnityEngine.Object.DestroyImmediate(catalog);
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 8,
                    Height = 8
                },
                world);
        }

        private static BattleViewCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<BattleViewCatalog>();
            SetField(catalog, "cellSize", new Vector2(1f, 1f));
            SetField(catalog, "origin", Vector3.zero);
            return catalog;
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

        private static LineRenderer FindRenderer(LineRenderer[] renderers, string namePrefix)
        {
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer != null &&
                    renderer.gameObject.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    return renderer;
                }
            }

            return null;
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

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
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
