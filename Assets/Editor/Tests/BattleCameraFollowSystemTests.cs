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
    public sealed class BattleCameraFollowSystemTests
    {
        [Test]
        public void CameraFollowSystemCentersCameraOnPlayerControlledEntity()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -8f);
            var system = new BattleCameraFollowSystem(catalog);
            system.SetTargetCamera(camera);

            try
            {
                CreatePlayer(world, new float2(2f, 3f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.AreEqual(5f, camera.transform.position.x, 0.001f);
                Assert.AreEqual(10f, camera.transform.position.y, 0.001f);
                Assert.AreEqual(-8f, camera.transform.position.z, 0.001f);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(catalog);
                DisposeWorld(world);
            }
        }

        [Test]
        public void CameraFollowSystemKeepsCameraWhenPlayerIsMissing()
        {
            var world = World.Create();
            var catalog = CreateCatalog();
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(3f, 4f, -8f);
            var system = new BattleCameraFollowSystem(catalog);
            system.SetTargetCamera(camera);

            try
            {
                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.AreEqual(new Vector3(3f, 4f, -8f), camera.transform.position);
            }
            finally
            {
                system.Dispose();
                UnityEngine.Object.DestroyImmediate(cameraObject);
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
                    Width = 1,
                    Height = 1
                },
                world);
        }

        private static BattleViewCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<BattleViewCatalog>();
            SetField(catalog, "cellSize", new Vector2(2f, 3f));
            SetField(catalog, "origin", new Vector3(1f, 1f, 0f));
            return catalog;
        }

        private static void CreatePlayer(World world, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
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
