using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleObjectiveSystemTests
    {
        [Test]
        public void ObjectiveSystemCapturesControlPointForSinglePresentTeam()
        {
            var world = World.Create();
            var system = new BattleObjectiveSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1.5f, 1.5f), currentHealth: 10);
                world.Commit();

                var session = CreateSession(world);
                system.Initialize(session);
                system.Tick(8f);

                var state = session.ObjectiveStates[0];
                Assert.IsTrue(state.HasOwner);
                Assert.AreEqual(BattleTeamType.Attacker, state.OwnerTeam);
                Assert.IsTrue(state.HasCaptureTeam);
                Assert.AreEqual(BattleTeamType.Attacker, state.CaptureTeam);
                Assert.AreEqual(1f, state.CaptureProgress);
                Assert.IsFalse(state.IsContested);
                Assert.AreEqual(1, state.AttackerPresence);
                Assert.AreEqual(0, state.DefenderPresence);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ObjectiveSystemMarksContestedAndDoesNotAdvanceCapture()
        {
            var world = World.Create();
            var system = new BattleObjectiveSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1.5f, 1.5f), currentHealth: 10);
                CreateUnit(world, BattleTeamType.Defender, new float2(2.5f, 2.5f), currentHealth: 10);
                world.Commit();

                var session = CreateSession(world);
                system.Initialize(session);
                system.Tick(8f);

                var state = session.ObjectiveStates[0];
                Assert.IsFalse(state.HasOwner);
                Assert.IsFalse(state.HasCaptureTeam);
                Assert.AreEqual(0f, state.CaptureProgress);
                Assert.IsTrue(state.IsContested);
                Assert.AreEqual(1, state.AttackerPresence);
                Assert.AreEqual(1, state.DefenderPresence);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ObjectiveSystemIgnoresDeadAndZeroHealthUnits()
        {
            var world = World.Create();
            var system = new BattleObjectiveSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1.5f, 1.5f), currentHealth: 0);
                CreateUnit(world, BattleTeamType.Defender, new float2(2.5f, 2.5f), currentHealth: 10, dead: true);
                world.Commit();

                var session = CreateSession(world);
                system.Initialize(session);
                system.Tick(8f);

                var state = session.ObjectiveStates[0];
                Assert.IsFalse(state.HasOwner);
                Assert.IsFalse(state.HasCaptureTeam);
                Assert.AreEqual(0, state.AttackerPresence);
                Assert.AreEqual(0, state.DefenderPresence);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void ObjectiveSystemIgnoresDriversInsideVehicles()
        {
            var world = World.Create();
            var system = new BattleObjectiveSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, new float2(1.5f, 1.5f), currentHealth: 10, driver: true);
                world.Commit();

                var session = CreateSession(world);
                system.Initialize(session);
                system.Tick(8f);

                var state = session.ObjectiveStates[0];
                Assert.IsFalse(state.HasOwner);
                Assert.AreEqual(0, state.AttackerPresence);
                Assert.AreEqual(0, state.DefenderPresence);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 4,
                    Height = 4,
                    Objectives = new[]
                    {
                        new BattleObjectiveZone
                        {
                            Type = BattleObjectiveType.ControlPoint,
                            Area = new RectInt(1, 1, 2, 2),
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

            if (driver)
            {
                world.GetStash<DriverComponent>().Set(entity, new DriverComponent());
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
