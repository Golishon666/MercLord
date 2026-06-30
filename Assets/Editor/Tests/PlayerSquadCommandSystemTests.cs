using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.Input;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class PlayerSquadCommandSystemTests
    {
        [Test]
        public void FollowPlayerCommandUpdatesOnlyPlayerTeamSquads()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = CreateSnapshot(SquadOrderType.FollowPlayer)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 4f));
                var friendlySquad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(2f, 2f),
                    order: SquadOrderType.AttackNearest,
                    target: new float2(20f, 2f));
                var enemySquad = CreateSquad(
                    world,
                    BattleTeamType.Defender,
                    squadId: 2,
                    anchor: new float2(18f, 2f),
                    order: SquadOrderType.AttackNearest,
                    target: new float2(0f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var orders = world.GetStash<SquadOrderComponent>();
                var friendlyOrder = orders.Get(friendlySquad);
                var enemyOrder = orders.Get(enemySquad);

                Assert.AreEqual(SquadOrderType.FollowPlayer, friendlyOrder.Value);
                Assert.AreEqual(10f, friendlyOrder.TargetPosition.x);
                Assert.AreEqual(4f, friendlyOrder.TargetPosition.y);
                Assert.AreEqual(SquadOrderType.AttackNearest, enemyOrder.Value);
                Assert.AreEqual(0f, enemyOrder.TargetPosition.x);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void HoldPositionCommandTargetsCurrentSquadAnchor()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = CreateSnapshot(SquadOrderType.HoldPosition)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 4f));
                var squad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(5f, 6f),
                    order: SquadOrderType.AttackNearest,
                    target: new float2(20f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(SquadOrderType.HoldPosition, order.Value);
                Assert.AreEqual(5f, order.TargetPosition.x);
                Assert.AreEqual(6f, order.TargetPosition.y);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void RetreatCommandTargetsAwayFromPlayer()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = CreateSnapshot(SquadOrderType.Retreat)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 5f));
                var squad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(7f, 5f),
                    order: SquadOrderType.AttackNearest,
                    target: new float2(20f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(SquadOrderType.Retreat, order.Value);
                Assert.Less(order.TargetPosition.x, 7f);
                Assert.AreEqual(5f, order.TargetPosition.y);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void AttackCommandTargetsPrimaryObjectiveWhenAvailable()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = CreateSnapshot(SquadOrderType.AttackNearest)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 4f));
                var squad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(5f, 6f),
                    order: SquadOrderType.HoldPosition,
                    target: new float2(5f, 6f));
                world.Commit();

                system.Initialize(CreateSession(
                    world,
                    new[]
                    {
                        new BattleObjectiveZone
                        {
                            Type = BattleObjectiveType.ControlPoint,
                            Area = new UnityEngine.RectInt(6, 2, 4, 2),
                            Priority = 1
                        }
                    }));
                system.Tick(0f);

                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(SquadOrderType.AttackNearest, order.Value);
                Assert.AreEqual(8f, order.TargetPosition.x);
                Assert.AreEqual(3f, order.TargetPosition.y);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void AttackCommandKeepsCurrentTargetWhenNoObjectiveExists()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = CreateSnapshot(SquadOrderType.AttackNearest)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 4f));
                var squad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(5f, 6f),
                    order: SquadOrderType.HoldPosition,
                    target: new float2(20f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(SquadOrderType.AttackNearest, order.Value);
                Assert.AreEqual(20f, order.TargetPosition.x);
                Assert.AreEqual(2f, order.TargetPosition.y);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void CommandSystemIgnoresSnapshotWithoutCommandPress()
        {
            var world = World.Create();
            var input = new FakeBattleInputSource
            {
                Snapshot = new BattleInputSnapshot(
                    float2.zero,
                    float2.zero,
                    false,
                    false,
                    0,
                    squadOrderPressed: false,
                    squadOrder: SquadOrderType.HoldPosition)
            };
            var system = new PlayerSquadCommandSystem(input);

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker, new float2(10f, 4f));
                var squad = CreateSquad(
                    world,
                    BattleTeamType.Attacker,
                    squadId: 1,
                    anchor: new float2(5f, 6f),
                    order: SquadOrderType.AttackNearest,
                    target: new float2(20f, 2f));
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(SquadOrderType.AttackNearest, order.Value);
                Assert.AreEqual(20f, order.TargetPosition.x);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleInputSnapshot CreateSnapshot(SquadOrderType order)
        {
            return new BattleInputSnapshot(
                float2.zero,
                float2.zero,
                false,
                false,
                0,
                squadOrderPressed: true,
                squadOrder: order);
        }

        private static BattleSession CreateSession(World world, BattleObjectiveZone[] objectives = null)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 1,
                    Height = 1,
                    Objectives = objectives ?? System.Array.Empty<BattleObjectiveZone>()
                },
                world);
        }

        private static Entity CreatePlayer(World world, BattleTeamType team, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent { Value = team });
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent { Value = position });
            return entity;
        }

        private static Entity CreateSquad(
            World world,
            BattleTeamType team,
            int squadId,
            float2 anchor,
            SquadOrderType order,
            float2 target)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = squadId,
                UnitConfigId = 101,
                FactionId = 1,
                Team = team,
                MemberCount = 4
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

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }

        private sealed class FakeBattleInputSource : IBattleInputSource
        {
            public BattleInputSnapshot Snapshot { get; set; }
        }
    }
}
