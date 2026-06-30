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
    public sealed class ViewSyncSystemTests
    {
        [Test]
        public void ViewSyncMovesAndShowsFullLodView()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var factory = new FakeViewFactory();
            var view = new GameObject("full-lod-view");
            var system = new ViewSyncSystem(catalog, factory);

            try
            {
                factory.Register(1, view);
                var entity = CreateViewedEntity(world, new float2(2f, 3f), viewId: 1, BattleLodLevel.Full);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.IsTrue(view.activeSelf);
                Assert.AreEqual(2f, view.transform.position.x, 0.001f);
                Assert.AreEqual(3f, view.transform.position.y, 0.001f);
                Assert.IsTrue(world.Has(entity));
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(catalog);
                DisposeWorld(world);
            }
        }

        [Test]
        public void ViewSyncHidesStrategicAndKeepsDeadInfantryAsCorpseView()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var factory = new FakeViewFactory();
            var strategicView = new GameObject("strategic-view");
            var deadView = CreateInfantryView("dead-view");
            var system = new ViewSyncSystem(catalog, factory);

            try
            {
                factory.Register(1, strategicView);
                factory.Register(2, deadView.gameObject);
                CreateViewedEntity(world, new float2(2f, 3f), viewId: 1, BattleLodLevel.Strategic);
                CreateViewedEntity(world, new float2(3f, 4f), viewId: 2, BattleLodLevel.Full, dead: true);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.IsFalse(strategicView.activeSelf);
                Assert.IsTrue(deadView.gameObject.activeSelf);
                Assert.IsTrue(deadView.IsDeadVisual);
                Assert.IsTrue(deadView.BodyRenderer.enabled);
                Assert.IsFalse(deadView.HeadRenderer.enabled);
                Assert.IsFalse(deadView.WeaponRenderer.enabled);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(strategicView);
                UnityEngine.Object.DestroyImmediate(deadView.gameObject);
                UnityEngine.Object.DestroyImmediate(catalog);
                DisposeWorld(world);
            }
        }

        [Test]
        public void ViewSyncRestoresInfantryVisualWhenEntityReturnsToFullLod()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var factory = new FakeViewFactory();
            var view = CreateInfantryView("restored-view");
            var system = new ViewSyncSystem(catalog, factory);

            try
            {
                view.SetDeadVisual(true);
                factory.Register(1, view.gameObject);
                CreateViewedEntity(world, new float2(2f, 3f), viewId: 1, BattleLodLevel.Full);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.IsTrue(view.gameObject.activeSelf);
                Assert.IsFalse(view.IsDeadVisual);
                Assert.IsTrue(view.BodyRenderer.enabled);
                Assert.IsTrue(view.HeadRenderer.enabled);
                Assert.IsTrue(view.WeaponRenderer.enabled);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(view.gameObject);
                UnityEngine.Object.DestroyImmediate(catalog);
                DisposeWorld(world);
            }
        }

        [Test]
        public void ViewSyncHidesDrivenInfantryView()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var factory = new FakeViewFactory();
            var view = CreateInfantryView("driven-view");
            var system = new ViewSyncSystem(catalog, factory);

            try
            {
                factory.Register(1, view.gameObject);
                CreateViewedEntity(world, new float2(2f, 3f), viewId: 1, BattleLodLevel.Full, driven: true);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.IsFalse(view.gameObject.activeSelf);
                Assert.IsFalse(view.IsDeadVisual);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(view.gameObject);
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

        private static InfantryView CreateInfantryView(string name)
        {
            var root = new GameObject(name);
            var view = root.AddComponent<InfantryView>();
            var body = CreateRendererChild(root.transform, "Body");
            var head = CreateRendererChild(root.transform, "Head");
            var weapon = CreateRendererChild(root.transform, "Weapon");
            SetField(view, "bodyRenderer", body);
            SetField(view, "headRenderer", head);
            SetField(view, "weaponRenderer", weapon);
            return view;
        }

        private static SpriteRenderer CreateRendererChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, worldPositionStays: false);
            return child.AddComponent<SpriteRenderer>();
        }

        private static Entity CreateViewedEntity(
            World world,
            float2 position,
            int viewId,
            BattleLodLevel lodLevel,
            bool dead = false,
            bool driven = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<ViewRefComponent>().Set(entity, new ViewRefComponent
            {
                ViewId = viewId
            });
            world.GetStash<BattleLodComponent>().Set(entity, new BattleLodComponent
            {
                Level = lodLevel,
                DistanceToFocus = 0f
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(entity, new DeadComponent());
            }

            if (driven)
            {
                world.GetStash<DriverComponent>().Set(entity, new DriverComponent());
            }

            return entity;
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

        private sealed class FakeViewFactory : IBattleViewFactory
        {
            private readonly Dictionary<int, GameObject> views = new Dictionary<int, GameObject>();

            public int SpawnUnitView(UnitConfig unitConfig, Transform parent)
            {
                throw new NotSupportedException();
            }

            public int SpawnVehicleView(VehicleConfig vehicleConfig, Transform parent)
            {
                throw new NotSupportedException();
            }

            public int SpawnProjectileView(Transform parent)
            {
                throw new NotSupportedException();
            }

            public bool TryGetView(int viewId, out GameObject view)
            {
                return views.TryGetValue(viewId, out view);
            }

            public void ReleaseView(int viewId)
            {
                views.Remove(viewId);
            }

            public void ReleaseAll()
            {
                views.Clear();
            }

            public void Register(int viewId, GameObject view)
            {
                views.Add(viewId, view);
            }
        }
    }
}
