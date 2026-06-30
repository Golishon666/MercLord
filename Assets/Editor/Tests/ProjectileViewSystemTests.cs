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
    public sealed class ProjectileViewSystemTests
    {
        [Test]
        public void ProjectileViewSystemSpawnsViewRefsForProjectiles()
        {
            var world = World.Create();
            var rootObject = new GameObject("battle-root");
            var viewRoot = new GameObject("view-root");
            var sceneRoot = rootObject.AddComponent<BattleSceneRoot>();
            var factory = new FakeViewFactory();
            var system = new ProjectileViewSystem(sceneRoot, factory);

            try
            {
                viewRoot.transform.SetParent(rootObject.transform);
                SetField(sceneRoot, "unitViewRoot", viewRoot.transform);
                var projectile = CreateProjectile(world, new float2(1f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.IsTrue(world.GetStash<ViewRefComponent>().Has(projectile));
                Assert.AreEqual(1, factory.SpawnedParents.Count);
                Assert.AreEqual(viewRoot.transform, factory.SpawnedParents[0]);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(rootObject);
                DisposeWorld(world);
            }
        }

        [Test]
        public void ProjectileViewSystemReleasesStaleProjectileViews()
        {
            var world = World.Create();
            var rootObject = new GameObject("battle-root");
            var viewRoot = new GameObject("view-root");
            var sceneRoot = rootObject.AddComponent<BattleSceneRoot>();
            var factory = new FakeViewFactory();
            var system = new ProjectileViewSystem(sceneRoot, factory);

            try
            {
                viewRoot.transform.SetParent(rootObject.transform);
                SetField(sceneRoot, "unitViewRoot", viewRoot.transform);
                var projectile = CreateProjectile(world, new float2(1f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                world.RemoveEntity(projectile);
                world.Commit();

                system.Tick(0f);

                Assert.AreEqual(1, factory.ReleasedViewIds.Count);
                Assert.AreEqual(1, factory.ReleasedViewIds[0]);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(rootObject);
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 4,
                    Height = 4
                },
                world);
        }

        private static Entity CreateProjectile(World world, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<ProjectileComponent>().Set(entity, new ProjectileComponent
            {
                Damage = 1,
                Speed = 1f
            });
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
            private int nextViewId = 1;

            public List<Transform> SpawnedParents { get; } = new List<Transform>();
            public List<int> ReleasedViewIds { get; } = new List<int>();

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
                SpawnedParents.Add(parent);
                return nextViewId++;
            }

            public bool TryGetView(int viewId, out GameObject view)
            {
                view = null;
                return false;
            }

            public void ReleaseView(int viewId)
            {
                ReleasedViewIds.Add(viewId);
            }

            public void ReleaseAll()
            {
            }
        }
    }
}
