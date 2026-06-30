using System.Linq;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattleMinimapHudPresenterTests
    {
        [Test]
        public void BuildSnapshotReportsInvalidWhenUnbound()
        {
            var presenter = new BattleMinimapHudPresenter();

            var snapshot = presenter.BuildSnapshot();

            Assert.IsFalse(snapshot.IsValid);
            Assert.AreEqual(0, snapshot.Pins.Count);
            Assert.AreEqual(0, snapshot.DangerZones.Count);
            Assert.AreEqual(0, snapshot.ObjectiveCount);
            Assert.AreEqual(0, snapshot.AttackerAlive);
            Assert.AreEqual(0, snapshot.DefenderAlive);
        }

        [Test]
        public void BuildSnapshotCountsAlivePinsAndPlayerTeam()
        {
            var world = World.Create();
            var presenter = new BattleMinimapHudPresenter();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(2f, 3f), currentHealth: 10, playerControlled: true);
                CreateUnit(world, BattleTeamType.Attacker, new float2(4f, 5f), currentHealth: 8);
                CreateUnit(world, BattleTeamType.Defender, new float2(6f, 7f), currentHealth: 9);
                CreateWarning(world, new float2(8f, 2f), radius: 2.5f, duration: 5f, remainingTime: 2.5f);
                world.Commit();

                var session = CreateSession(world, width: 12, height: 10);
                session.ObjectiveStates[0].HasCaptureTeam = true;
                session.ObjectiveStates[0].CaptureTeam = BattleTeamType.Defender;
                session.ObjectiveStates[0].CaptureProgress = 0.5f;
                session.ObjectiveStates[0].IsContested = true;
                presenter.Bind(session);
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.IsValid);
                Assert.AreEqual(12, snapshot.MapWidth);
                Assert.AreEqual(10, snapshot.MapHeight);
                Assert.AreEqual(1, snapshot.ObjectiveCount);
                Assert.IsTrue(snapshot.HasObjectiveCaptureTeam);
                Assert.AreEqual(BattleTeamType.Defender, snapshot.ObjectiveCaptureTeam);
                Assert.AreEqual(0.5f, snapshot.ObjectiveCaptureProgress);
                Assert.IsTrue(snapshot.IsObjectiveContested);
                Assert.IsTrue(snapshot.HasPlayer);
                Assert.AreEqual(BattleTeamType.Attacker, snapshot.PlayerTeam);
                Assert.AreEqual(2, snapshot.AttackerAlive);
                Assert.AreEqual(1, snapshot.DefenderAlive);
                Assert.AreEqual(3, snapshot.Pins.Count);
                Assert.IsTrue(snapshot.Pins.Any(pin => pin.IsPlayer));
                Assert.AreEqual(1, snapshot.DangerZones.Count);
                Assert.AreEqual(8f, snapshot.DangerZones[0].Position.x, 0.001f);
                Assert.AreEqual(2f, snapshot.DangerZones[0].Position.y, 0.001f);
                Assert.AreEqual(2.5f, snapshot.DangerZones[0].Radius, 0.001f);
                Assert.AreEqual(0.5f, snapshot.DangerZones[0].RemainingFraction, 0.001f);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotSkipsDeadAndZeroHealthUnits()
        {
            var world = World.Create();
            var presenter = new BattleMinimapHudPresenter();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1f, 1f), currentHealth: 10, dead: true);
                CreateUnit(world, BattleTeamType.Attacker, new float2(2f, 2f), currentHealth: 0);
                CreateUnit(world, BattleTeamType.Defender, new float2(3f, 3f), currentHealth: 4);
                world.Commit();

                presenter.Bind(CreateSession(world, width: 8, height: 8));
                var snapshot = presenter.BuildSnapshot();

                Assert.AreEqual(0, snapshot.AttackerAlive);
                Assert.AreEqual(1, snapshot.DefenderAlive);
                Assert.AreEqual(1, snapshot.Pins.Count);
                Assert.AreEqual(BattleTeamType.Defender, snapshot.Pins[0].Team);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotSkipsDriversInsideVehicles()
        {
            var world = World.Create();
            var presenter = new BattleMinimapHudPresenter();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1f, 1f), currentHealth: 10, driver: true);
                CreateUnit(world, BattleTeamType.Attacker, new float2(2f, 2f), currentHealth: 10);
                world.Commit();

                presenter.Bind(CreateSession(world, width: 8, height: 8));
                var snapshot = presenter.BuildSnapshot();

                Assert.AreEqual(1, snapshot.AttackerAlive);
                Assert.AreEqual(0, snapshot.DefenderAlive);
                Assert.AreEqual(1, snapshot.Pins.Count);
                Assert.AreEqual(new float2(2f, 2f), snapshot.Pins[0].Position);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, int width, int height)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = width,
                    Height = height,
                    Objectives = new[]
                    {
                        new BattleObjectiveZone
                        {
                            Type = BattleObjectiveType.ControlPoint,
                            Area = new UnityEngine.RectInt(width / 2, height / 2, 1, 1),
                            Priority = 1
                        }
                    }
                },
                world);
        }

        private static void CreateUnit(
            World world,
            BattleTeamType team,
            float2 position,
            int currentHealth,
            bool dead = false,
            bool playerControlled = false,
            bool driver = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
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

            if (playerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            }

            if (driver)
            {
                world.GetStash<DriverComponent>().Set(entity, new DriverComponent());
            }
        }

        private static void CreateWarning(
            World world,
            float2 position,
            float radius,
            float duration,
            float remainingTime)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<ArtilleryWarningComponent>().Set(entity, new ArtilleryWarningComponent
            {
                Radius = radius,
                Duration = duration,
                RemainingTime = remainingTime
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
