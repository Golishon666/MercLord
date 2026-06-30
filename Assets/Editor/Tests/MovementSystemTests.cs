using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class MovementSystemTests
    {
        [Test]
        public void InfantryMovesOntoInfantryTile()
        {
            var model = CreateModel(3, 1, MoveLayer.Infantry);
            var world = World.Create();
            var system = new MovementSystem();

            try
            {
                var entity = CreateMover(world, new float2(0.5f, 0.5f), new float2(1f, 0f), vehicle: false);
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(1f);

                var position = world.GetStash<PositionComponent>().Get(entity).Value;
                Assert.AreEqual(1.5f, position.x, 0.001f);
                Assert.AreEqual(0.5f, position.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VehicleCannotMoveOntoInfantryOnlyTile()
        {
            var model = CreateModel(3, 1, MoveLayer.Infantry | MoveLayer.Vehicle);
            SetTile(model, 1, 0, walkable: true, MoveLayer.Infantry);
            var world = World.Create();
            var system = new MovementSystem();

            try
            {
                var entity = CreateMover(world, new float2(0.5f, 0.5f), new float2(1f, 0f), vehicle: true);
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(1f);

                var position = world.GetStash<PositionComponent>().Get(entity).Value;
                var velocity = world.GetStash<VelocityComponent>().Get(entity).Value;
                Assert.AreEqual(0.5f, position.x, 0.001f);
                Assert.AreEqual(0.5f, position.y, 0.001f);
                Assert.AreEqual(0f, velocity.x, 0.001f);
                Assert.AreEqual(0f, velocity.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VehicleMovesOntoVehicleTile()
        {
            var model = CreateModel(3, 1, MoveLayer.Infantry | MoveLayer.Vehicle);
            var world = World.Create();
            var system = new MovementSystem();

            try
            {
                var entity = CreateMover(world, new float2(0.5f, 0.5f), new float2(1f, 0f), vehicle: true);
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(1f);

                var position = world.GetStash<PositionComponent>().Get(entity).Value;
                Assert.AreEqual(1.5f, position.x, 0.001f);
                Assert.AreEqual(0.5f, position.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void MovementStopsAtObstacleAndMapBounds()
        {
            var model = CreateModel(2, 1, MoveLayer.Infantry | MoveLayer.Vehicle);
            SetTile(model, 1, 0, walkable: false, MoveLayer.None);
            var world = World.Create();
            var system = new MovementSystem();

            try
            {
                var entity = CreateMover(world, new float2(0.5f, 0.5f), new float2(1f, 0f), vehicle: false);
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(1f);

                var position = world.GetStash<PositionComponent>().Get(entity).Value;
                Assert.AreEqual(0.5f, position.x, 0.001f);
                Assert.AreEqual(0.5f, position.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(BattleModel model, World world)
        {
            return new BattleSession(new BattleGenerationRequest(), model, world);
        }

        private static BattleModel CreateModel(int width, int height, MoveLayer moveLayers)
        {
            var tiles = new BattleTile[width * height];
            for (var index = 0; index < tiles.Length; index++)
            {
                tiles[index] = new BattleTile
                {
                    Walkable = true,
                    Surface = BattleTileSurface.Ground,
                    MoveCost = 1,
                    AllowedMoveLayers = moveLayers
                };
            }

            return new BattleModel
            {
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static void SetTile(BattleModel model, int x, int y, bool walkable, MoveLayer moveLayers)
        {
            model.Tiles[y * model.Width + x] = new BattleTile
            {
                Walkable = walkable,
                Surface = walkable ? BattleTileSurface.Ground : BattleTileSurface.Obstacle,
                MoveCost = 1,
                AllowedMoveLayers = moveLayers
            };
        }

        private static Entity CreateMover(World world, float2 position, float2 velocity, bool vehicle)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = velocity
            });
            world.GetStash<MovementStatsComponent>().Set(entity, new MovementStatsComponent
            {
                MoveSpeed = 1f,
                RotationSpeed = 1f
            });

            if (vehicle)
            {
                world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
                {
                    VehicleConfigId = 1,
                    State = VehicleStateType.AIControlled
                });
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
