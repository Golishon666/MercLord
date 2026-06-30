using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattleSquadMovementSystemTests
    {
        [Test]
        public void SquadMovementUsesFlowFieldAroundObstacle()
        {
            var model = CreateModel(5, 5);
            Block(model, 1, 1);
            Block(model, 1, 2);
            Block(model, 1, 3);
            var world = World.Create();
            var system = new SquadMovementSystem();

            try
            {
                CreateSquad(world, squadId: 7, anchor: new float2(0.5f, 2.5f), target: new float2(4.5f, 2.5f));
                var member = CreateMember(world, squadId: 7, position: new float2(0.5f, 2.5f), slotOffset: float2.zero);
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(member).Value;
                Assert.Less(math.abs(velocity.x), 0.01f);
                Assert.Greater(math.abs(velocity.y), 0.99f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SquadMovementAppliesFormationSlotCorrection()
        {
            var model = CreateModel(5, 5);
            var world = World.Create();
            var system = new SquadMovementSystem();

            try
            {
                CreateSquad(
                    world,
                    squadId: 3,
                    anchor: new float2(1f, 1f),
                    target: new float2(4f, 1f),
                    order: SquadOrderType.HoldPosition);
                var member = CreateMember(world, squadId: 3, position: new float2(1f, 1f), slotOffset: new float2(2f, 0f));
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(member).Value;
                Assert.Greater(velocity.x, 0.99f);
                Assert.Less(math.abs(velocity.y), 0.01f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SquadMovementDoesNotOverridePlayerControlledUnits()
        {
            var model = CreateModel(5, 5);
            var world = World.Create();
            var system = new SquadMovementSystem();

            try
            {
                CreateSquad(world, squadId: 5, anchor: new float2(0.5f, 0.5f), target: new float2(4.5f, 0.5f));
                var player = CreateMember(
                    world,
                    squadId: 5,
                    position: new float2(0.5f, 0.5f),
                    slotOffset: new float2(2f, 0f),
                    playerControlled: true,
                    velocity: new float2(0.25f, 0.75f));
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(player).Value;
                Assert.AreEqual(0.25f, velocity.x);
                Assert.AreEqual(0.75f, velocity.y);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void SquadMovementDoesNotAdvanceStoppedUnitWithCombatTarget()
        {
            var model = CreateModel(5, 5);
            var world = World.Create();
            var system = new SquadMovementSystem();

            try
            {
                CreateSquad(world, squadId: 9, anchor: new float2(0.5f, 0.5f), target: new float2(4.5f, 0.5f));
                var member = CreateMember(world, squadId: 9, position: new float2(0.5f, 0.5f), slotOffset: float2.zero);
                var target = world.CreateEntity();
                world.GetStash<TargetComponent>().Set(member, new TargetComponent { Target = target });
                world.Commit();

                system.Initialize(CreateSession(model, world));
                system.Tick(0f);

                var velocity = world.GetStash<VelocityComponent>().Get(member).Value;
                Assert.AreEqual(0f, velocity.x);
                Assert.AreEqual(0f, velocity.y);
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

        private static BattleModel CreateModel(int width, int height)
        {
            var tiles = new BattleTile[width * height];
            for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
            {
                tiles[tileIndex] = new BattleTile
                {
                    Walkable = true,
                    Surface = BattleTileSurface.Ground,
                    MoveCost = 1,
                    AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle
                };
            }

            return new BattleModel
            {
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static void Block(BattleModel model, int x, int y)
        {
            model.Tiles[y * model.Width + x] = new BattleTile
            {
                Walkable = false,
                Surface = BattleTileSurface.Obstacle,
                MoveCost = 1,
                AllowedMoveLayers = MoveLayer.None,
                BlocksLineOfSight = true,
                BlocksProjectiles = true
            };
        }

        private static Entity CreateSquad(
            World world,
            int squadId,
            float2 anchor,
            float2 target,
            SquadOrderType order = SquadOrderType.AttackNearest)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = squadId,
                UnitConfigId = 101,
                FactionId = 1,
                Team = BattleTeamType.Attacker,
                MemberCount = 1
            });
            world.GetStash<SquadAnchorComponent>().Set(entity, new SquadAnchorComponent
            {
                Position = anchor,
                ForwardDirection = new float2(1f, 0f)
            });
            world.GetStash<SquadOrderComponent>().Set(entity, new SquadOrderComponent
            {
                Value = order,
                TargetPosition = target
            });
            return entity;
        }

        private static Entity CreateMember(
            World world,
            int squadId,
            float2 position,
            float2 slotOffset,
            bool playerControlled = false,
            float2 velocity = default)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadMemberComponent>().Set(entity, new SquadMemberComponent
            {
                SquadId = squadId,
                SlotIndex = 0,
                SquadSize = 1
            });
            world.GetStash<FormationSlotComponent>().Set(entity, new FormationSlotComponent
            {
                LocalOffset = slotOffset
            });
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = velocity
            });

            if (playerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
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
