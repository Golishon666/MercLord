using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace MercLord.Editor.Tests
{
    public sealed class BattleStatusHudPresenterTests
    {
        [Test]
        public void BuildSnapshotCountsTeamsLossesAndPlayerHealth()
        {
            var world = World.Create();
            var presenter = new BattleStatusHudPresenter();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, currentHealth: 25, maxHealth: 40, playerControlled: true);
                CreateUnit(world, BattleTeamType.Attacker, currentHealth: 0, maxHealth: 10, dead: true);
                CreateUnit(world, BattleTeamType.Defender, currentHealth: 10, maxHealth: 10);
                CreateUnit(world, BattleTeamType.Defender, currentHealth: 1, maxHealth: 10);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.AreEqual(2, snapshot.Attacker.Total);
                Assert.AreEqual(1, snapshot.Attacker.Alive);
                Assert.AreEqual(1, snapshot.Attacker.Lost);
                Assert.AreEqual(2, snapshot.Defender.Total);
                Assert.AreEqual(2, snapshot.Defender.Alive);
                Assert.AreEqual(0, snapshot.Defender.Lost);
                Assert.IsTrue(snapshot.HasPlayer);
                Assert.AreEqual(BattleTeamType.Attacker, snapshot.PlayerTeam);
                Assert.AreEqual(25, snapshot.PlayerCurrentHealth);
                Assert.AreEqual(40, snapshot.PlayerMaxHealth);
                Assert.IsFalse(snapshot.IsCompleted);
                Assert.AreEqual(BattleOutcome.None, snapshot.Outcome);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsCompletedOutcome()
        {
            var world = World.Create();
            var presenter = new BattleStatusHudPresenter();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, currentHealth: 0, maxHealth: 10, dead: true);
                CreateUnit(world, BattleTeamType.Defender, currentHealth: 10, maxHealth: 10);
                world.Commit();

                var session = CreateSession(world);
                session.Completion.Complete(new BattleResult
                {
                    Outcome = BattleOutcome.DefenderVictory
                });

                presenter.Bind(session);
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.IsCompleted);
                Assert.AreEqual(BattleOutcome.DefenderVictory, snapshot.Outcome);
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

        private static void CreateUnit(
            World world,
            BattleTeamType team,
            int currentHealth,
            int maxHealth,
            bool dead = false,
            bool playerControlled = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = currentHealth,
                Max = maxHealth
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(entity, new DeadComponent());
            }

            if (playerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
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
