using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleCameraShakeSystemTests
    {
        [Test]
        public void CameraShakeSystemAppliesOffsetAndClearsExpiredShake()
        {
            var world = World.Create();
            var cameraObject = new GameObject("Battle Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 3f, -10f);
            var system = new BattleCameraShakeSystem();
            system.SetTargetCamera(camera);

            try
            {
                CreateShake(world);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                Assert.AreNotEqual(new Vector3(2f, 3f, -10f), camera.transform.position);

                system.Tick(1f);
                world.Commit();

                Assert.AreEqual(new Vector3(2f, 3f, -10f), camera.transform.position);
                Assert.AreEqual(0, CountShakes(world));
            }
            finally
            {
                system.Dispose();
                Object.DestroyImmediate(cameraObject);
                DisposeWorld(world);
            }
        }

        [Test]
        public void CameraShakeSystemUsesExternallyMovedBasePosition()
        {
            var world = World.Create();
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 3f, -10f);
            var system = new BattleCameraShakeSystem();
            system.SetTargetCamera(camera);

            try
            {
                CreateShake(world);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                camera.transform.position = new Vector3(10f, 11f, -10f);
                system.Tick(0.01f);

                Assert.Greater(camera.transform.position.y, 11.1f);
                Assert.AreEqual(-10f, camera.transform.position.z, 0.001f);
            }
            finally
            {
                system.Dispose();
                Object.DestroyImmediate(cameraObject);
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

        private static void CreateShake(World world)
        {
            var entity = world.CreateEntity();
            world.GetStash<BattleCameraShakeComponent>().Set(entity, new BattleCameraShakeComponent
            {
                Position = float2.zero,
                Intensity = 0.2f,
                Duration = 0.25f,
                RemainingTime = 0.25f
            });
        }

        private static int CountShakes(World world)
        {
            var filter = world.Filter
                .With<BattleCameraShakeComponent>()
                .Build();
            var count = 0;
            foreach (var entity in filter)
            {
                count++;
            }

            filter.Dispose();
            return count;
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
