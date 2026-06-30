using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattleLodSystemTests
    {
        [Test]
        public void LodSystemAssignsLevelsFromPlayerFocus()
        {
            var world = World.Create();
            var system = new BattleLodSystem();

            try
            {
                var player = CreateEntity(world, float2.zero, playerControlled: true);
                var near = CreateEntity(world, new float2(10f, 0f));
                var mid = CreateEntity(world, new float2(30f, 0f));
                var far = CreateEntity(world, new float2(60f, 0f));
                var dead = CreateEntity(world, new float2(2f, 0f), dead: true);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var lods = world.GetStash<BattleLodComponent>();
                Assert.AreEqual(BattleLodLevel.Full, lods.Get(player).Level);
                Assert.AreEqual(BattleLodLevel.Full, lods.Get(near).Level);
                Assert.AreEqual(BattleLodLevel.Simplified, lods.Get(mid).Level);
                Assert.AreEqual(BattleLodLevel.Strategic, lods.Get(far).Level);
                Assert.AreEqual(BattleLodLevel.Dead, lods.Get(dead).Level);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LodSystemUsesMapCenterWhenPlayerFocusIsMissing()
        {
            var world = World.Create();
            var system = new BattleLodSystem();

            try
            {
                var center = CreateEntity(world, new float2(50f, 50f));
                var far = CreateEntity(world, new float2(0f, 0f));
                world.Commit();

                system.Initialize(CreateSession(world, width: 100, height: 100));
                system.Tick(0f);

                var lods = world.GetStash<BattleLodComponent>();
                Assert.AreEqual(BattleLodLevel.Full, lods.Get(center).Level);
                Assert.AreEqual(BattleLodLevel.Strategic, lods.Get(far).Level);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LodSystemRunsAtFixedCadence()
        {
            var world = World.Create();
            var system = new BattleLodSystem();

            try
            {
                var player = CreateEntity(world, float2.zero, playerControlled: true);
                var target = CreateEntity(world, new float2(60f, 0f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                Assert.AreEqual(BattleLodLevel.Strategic, world.GetStash<BattleLodComponent>().Get(target).Level);

                ref var targetPosition = ref world.GetStash<PositionComponent>().Get(target);
                targetPosition.Value = new float2(1f, 0f);
                system.Tick(0.1f);
                Assert.AreEqual(BattleLodLevel.Strategic, world.GetStash<BattleLodComponent>().Get(target).Level);

                system.Tick(0.15f);
                Assert.AreEqual(BattleLodLevel.Full, world.GetStash<BattleLodComponent>().Get(target).Level);
                Assert.AreEqual(BattleLodLevel.Full, world.GetStash<BattleLodComponent>().Get(player).Level);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, int width = 128, int height = 128)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = width,
                    Height = height
                },
                world);
        }

        private static Entity CreateEntity(
            World world,
            float2 position,
            bool playerControlled = false,
            bool dead = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });

            if (playerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            }

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
    }
}
