using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace MercLord.Editor.Tests
{
    public sealed class BattleCommandHudPresenterTests
    {
        [Test]
        public void BuildSnapshotReportsNoPlayer()
        {
            var world = World.Create();
            var presenter = new BattleCommandHudPresenter();

            try
            {
                presenter.Bind(CreateSession(world));

                var snapshot = presenter.BuildSnapshot();

                Assert.IsFalse(snapshot.HasPlayer);
                Assert.IsFalse(snapshot.HasSquads);
                Assert.AreEqual(0, snapshot.FriendlySquadCount);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsCurrentPlayerTeamOrder()
        {
            var world = World.Create();
            var presenter = new BattleCommandHudPresenter();

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker);
                CreateSquad(world, BattleTeamType.Attacker, SquadOrderType.HoldPosition);
                CreateSquad(world, BattleTeamType.Defender, SquadOrderType.Retreat);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.HasPlayer);
                Assert.IsTrue(snapshot.HasSquads);
                Assert.AreEqual(SquadOrderType.HoldPosition, snapshot.CurrentOrder);
                Assert.AreEqual(1, snapshot.FriendlySquadCount);
                Assert.AreEqual(0, snapshot.MixedOrderCount);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsMixedFriendlyOrders()
        {
            var world = World.Create();
            var presenter = new BattleCommandHudPresenter();

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker);
                CreateSquad(world, BattleTeamType.Attacker, SquadOrderType.FollowPlayer);
                CreateSquad(world, BattleTeamType.Attacker, SquadOrderType.Retreat);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.HasSquads);
                Assert.AreEqual(SquadOrderType.FollowPlayer, snapshot.CurrentOrder);
                Assert.AreEqual(2, snapshot.FriendlySquadCount);
                Assert.AreEqual(1, snapshot.MixedOrderCount);
            }
            finally
            {
                presenter.Dispose();
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

        private static void CreatePlayer(World world, BattleTeamType team)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent { Value = team });
        }

        private static void CreateSquad(World world, BattleTeamType team, SquadOrderType order)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = team == BattleTeamType.Attacker ? 1 : 2,
                UnitConfigId = 101,
                FactionId = 1,
                Team = team,
                MemberCount = 4
            });
            world.GetStash<SquadOrderComponent>().Set(entity, new SquadOrderComponent
            {
                Value = order
            });
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
