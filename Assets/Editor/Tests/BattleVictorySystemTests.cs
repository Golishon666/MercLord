using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Global.Cells;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace MercLord.Editor.Tests
{
    public sealed class BattleVictorySystemTests
    {
        [Test]
        public void VictorySystemCompletesAttackerVictoryWhenDefendersAreDead()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: false, playerControlled: true);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: true);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 9);
                system.Initialize(session);
                system.Tick(0f);

                Assert.IsTrue(session.Completion.IsCompleted);
                Assert.AreEqual(BattleOutcome.AttackerVictory, session.Completion.Result.Outcome);
                Assert.AreEqual(11, session.Completion.Result.WinnerFactionId);
                Assert.AreEqual(9, session.Completion.Result.SourceCellId);
                Assert.IsTrue(session.Completion.Result.PlayerSurvived);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VictorySystemCompletesDefenderVictoryWhenAttackersAreDead()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: true, playerControlled: true);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: false);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 12);
                system.Initialize(session);
                system.Tick(0f);

                Assert.IsTrue(session.Completion.IsCompleted);
                Assert.AreEqual(BattleOutcome.DefenderVictory, session.Completion.Result.Outcome);
                Assert.AreEqual(22, session.Completion.Result.WinnerFactionId);
                Assert.IsFalse(session.Completion.Result.PlayerSurvived);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VictorySystemCompletesRetreatWhenBothTeamsAreDead()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: true);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: true);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 13);
                system.Initialize(session);
                system.Tick(0f);

                Assert.IsTrue(session.Completion.IsCompleted);
                Assert.AreEqual(BattleOutcome.Retreat, session.Completion.Result.Outcome);
                Assert.AreEqual(WorldIds.None, session.Completion.Result.WinnerFactionId);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VictorySystemDoesNotOverwriteCompletedResult()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                var attacker = CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: false);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: true);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 14);
                system.Initialize(session);
                system.Tick(0f);
                var firstResult = session.Completion.Result;

                world.GetStash<DeadComponent>().Set(attacker, new DeadComponent());
                world.Commit();
                system.Tick(0f);

                Assert.AreSame(firstResult, session.Completion.Result);
                Assert.AreEqual(BattleOutcome.AttackerVictory, session.Completion.Result.Outcome);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VictorySystemDoesNotCompleteWhenBothTeamsAreAliveWithoutObjectiveWinner()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: false);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: false);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 15);
                system.Initialize(session);
                system.Tick(0f);

                Assert.IsFalse(session.Completion.IsCompleted);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void VictorySystemCompletesObjectiveVictoryWhenControlPointIsOwned()
        {
            var world = World.Create();
            var system = CreateSystem();

            try
            {
                CreateUnit(world, BattleTeamType.Attacker, factionId: 11, dead: false);
                CreateUnit(world, BattleTeamType.Defender, factionId: 22, dead: false);
                world.Commit();

                var session = CreateSession(world, sourceCellId: 16, withObjective: true);
                session.ObjectiveStates[0].HasOwner = true;
                session.ObjectiveStates[0].OwnerTeam = BattleTeamType.Defender;
                system.Initialize(session);
                system.Tick(0f);

                Assert.IsTrue(session.Completion.IsCompleted);
                Assert.AreEqual(BattleOutcome.DefenderVictory, session.Completion.Result.Outcome);
                Assert.AreEqual(22, session.Completion.Result.WinnerFactionId);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, int sourceCellId, bool withObjective = false)
        {
            return new BattleSession(
                new BattleGenerationRequest
                {
                    SourceCellId = sourceCellId
                },
                new BattleModel
                {
                    Width = 1,
                    Height = 1,
                    Objectives = withObjective
                        ? new[]
                        {
                            new BattleObjectiveZone
                            {
                                Type = BattleObjectiveType.ControlPoint,
                                Area = new UnityEngine.RectInt(0, 0, 1, 1),
                                Priority = 1
                            }
                        }
                        : System.Array.Empty<BattleObjectiveZone>()
                },
                world);
        }

        private static BattleVictorySystem CreateSystem()
        {
            return new BattleVictorySystem(new BattleResultBuilder());
        }

        private static Entity CreateUnit(
            World world,
            BattleTeamType team,
            int factionId,
            bool dead,
            bool playerControlled = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });
            world.GetStash<FactionComponent>().Set(entity, new FactionComponent
            {
                Value = factionId
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = dead ? 0 : 10,
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
