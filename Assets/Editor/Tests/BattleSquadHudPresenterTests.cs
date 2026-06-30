using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace MercLord.Editor.Tests
{
    public sealed class BattleSquadHudPresenterTests
    {
        [Test]
        public void BuildSnapshotReportsNoPlayer()
        {
            var world = World.Create();
            var presenter = new BattleSquadHudPresenter();

            try
            {
                CreateSquad(
                    world,
                    squadId: 1,
                    team: BattleTeamType.Attacker,
                    order: SquadOrderType.AttackNearest,
                    memberCount: 3);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsFalse(snapshot.HasPlayer);
                Assert.IsFalse(snapshot.HasSquads);
                Assert.AreEqual(0, snapshot.Squads.Count);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsFriendlySquadAliveCountsAndOrders()
        {
            var world = World.Create();
            var presenter = new BattleSquadHudPresenter();

            try
            {
                CreatePlayer(world, BattleTeamType.Attacker);
                CreateSquad(
                    world,
                    squadId: 1,
                    team: BattleTeamType.Attacker,
                    order: SquadOrderType.FollowPlayer,
                    memberCount: 3);
                CreateSquad(
                    world,
                    squadId: 10000,
                    team: BattleTeamType.Defender,
                    order: SquadOrderType.AttackNearest,
                    memberCount: 1);
                CreateMember(world, squadId: 1, currentHealth: 10);
                CreateMember(world, squadId: 1, currentHealth: 0);
                CreateMember(world, squadId: 1, currentHealth: 10, dead: true);
                CreateMember(world, squadId: 10000, currentHealth: 10);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.HasPlayer);
                Assert.AreEqual(BattleTeamType.Attacker, snapshot.PlayerTeam);
                Assert.AreEqual(1, snapshot.Squads.Count);

                var entry = snapshot.Squads[0];
                Assert.AreEqual(1, entry.SquadId);
                Assert.AreEqual(501, entry.UnitConfigId);
                Assert.AreEqual(1, entry.AliveCount);
                Assert.AreEqual(3, entry.TotalCount);
                Assert.IsTrue(entry.HasOrder);
                Assert.AreEqual(SquadOrderType.FollowPlayer, entry.Order);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotSortsFriendlySquadsBySquadId()
        {
            var world = World.Create();
            var presenter = new BattleSquadHudPresenter();

            try
            {
                CreatePlayer(world, BattleTeamType.Defender);
                CreateSquad(
                    world,
                    squadId: 10005,
                    team: BattleTeamType.Defender,
                    order: SquadOrderType.HoldPosition,
                    memberCount: 1);
                CreateSquad(
                    world,
                    squadId: 10001,
                    team: BattleTeamType.Defender,
                    order: SquadOrderType.Retreat,
                    memberCount: 1);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.AreEqual(2, snapshot.Squads.Count);
                Assert.AreEqual(10001, snapshot.Squads[0].SquadId);
                Assert.AreEqual(10005, snapshot.Squads[1].SquadId);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsPlayerControlledVehicle()
        {
            var world = World.Create();
            var presenter = new BattleSquadHudPresenter();

            try
            {
                CreatePlayerVehicle(world, BattleTeamType.Attacker, vehicleConfigId: 700, currentHealth: 42, maxHealth: 100);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.HasPlayer);
                Assert.IsTrue(snapshot.HasVehicles);
                Assert.AreEqual(1, snapshot.Vehicles.Count);
                Assert.AreEqual(700, snapshot.Vehicles[0].VehicleConfigId);
                Assert.AreEqual(42, snapshot.Vehicles[0].CurrentHealth);
                Assert.AreEqual(100, snapshot.Vehicles[0].MaxHealth);
                Assert.AreEqual(VehicleStateType.PlayerControlled, snapshot.Vehicles[0].State);
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

        private static void CreatePlayerVehicle(
            World world,
            BattleTeamType team,
            int vehicleConfigId,
            int currentHealth,
            int maxHealth)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent { Value = team });
            world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
            {
                VehicleConfigId = vehicleConfigId,
                State = VehicleStateType.PlayerControlled
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = currentHealth,
                Max = maxHealth
            });
        }

        private static void CreateSquad(
            World world,
            int squadId,
            BattleTeamType team,
            SquadOrderType order,
            int memberCount)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = squadId,
                UnitConfigId = 501,
                FactionId = team == BattleTeamType.Attacker ? 1 : 2,
                Team = team,
                MemberCount = memberCount
            });
            world.GetStash<SquadOrderComponent>().Set(entity, new SquadOrderComponent
            {
                Value = order
            });
        }

        private static void CreateMember(World world, int squadId, int currentHealth, bool dead = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadMemberComponent>().Set(entity, new SquadMemberComponent
            {
                SquadId = squadId,
                SlotIndex = 0,
                SquadSize = 1
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = currentHealth,
                Max = 10
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(entity, new DeadComponent());
            }
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
