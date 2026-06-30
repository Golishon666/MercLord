using System;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class HitscanTraceSystemTests
    {
        [Test]
        public void HitscanTraceSystemRemovesExpiredTraces()
        {
            var world = World.Create();
            var system = new HitscanTraceSystem();

            try
            {
                var trace = CreateTrace(world, remainingTime: 0.05f);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0.02f);
                world.Commit();

                Assert.IsTrue(world.Has(trace));
                Assert.AreEqual(0.03f, world.GetStash<HitscanTraceComponent>().Get(trace).RemainingTime, 0.001f);

                system.Tick(0.031f);
                world.Commit();

                Assert.IsFalse(world.Has(trace));
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void HitscanTraceViewSystemRendersAndRemovesTraceLine()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var system = new HitscanTraceViewSystem(catalog);

            try
            {
                var trace = CreateTrace(world, remainingTime: 0.08f);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var root = GameObject.Find("Battle Hitscan Trace Views");
                Assert.IsNotNull(root);

                var renderers = root.GetComponentsInChildren<LineRenderer>();
                Assert.AreEqual(2, renderers.Length);
                var trail = Array.Find(renderers, candidate => candidate.gameObject.name == "Trail");
                var muzzle = Array.Find(renderers, candidate => candidate.gameObject.name == "Muzzle");
                Assert.IsNotNull(trail);
                Assert.IsNotNull(muzzle);
                Assert.AreEqual(2, trail.positionCount);
                Assert.AreEqual(3f, trail.GetPosition(0).x, 0.001f);
                Assert.AreEqual(7f, trail.GetPosition(0).y, 0.001f);
                Assert.AreEqual(5f, trail.GetPosition(1).x, 0.001f);
                Assert.AreEqual(10f, trail.GetPosition(1).y, 0.001f);
                Assert.AreEqual(3f, muzzle.GetPosition(0).x, 0.001f);
                Assert.AreEqual(7f, muzzle.GetPosition(0).y, 0.001f);
                Assert.Greater(muzzle.GetPosition(1).x, muzzle.GetPosition(0).x);
                Assert.Greater(muzzle.GetPosition(1).y, muzzle.GetPosition(0).y);

                world.RemoveEntity(trace);
                world.Commit();
                system.Tick(0f);

                Assert.IsEmpty(root.GetComponentsInChildren<LineRenderer>());
            }
            finally
            {
                system.Dispose();
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

        private static Entity CreateTrace(World world, float remainingTime)
        {
            var entity = world.CreateEntity();
            world.GetStash<HitscanTraceComponent>().Set(entity, new HitscanTraceComponent
            {
                Start = new float2(1f, 2f),
                End = new float2(2f, 3f),
                Duration = 0.08f,
                RemainingTime = remainingTime,
                Hit = true
            });
            return entity;
        }

        private static BattleViewCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<BattleViewCatalog>();
            SetField(catalog, "cellSize", new Vector2(2f, 3f));
            SetField(catalog, "origin", new Vector3(1f, 1f, 0f));
            return catalog;
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
    }
}
