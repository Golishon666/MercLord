using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Global.Cells;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class BattleVictorySystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 1f;

        private readonly IBattleResultBuilder resultBuilder;

        private BattleSession session;
        private World world;
        private Filter filter;
        private Stash<TeamComponent> teams;
        private Stash<FactionComponent> factions;
        private Stash<DeadComponent> dead;
        private BattleCadenceTimer tickTimer;

        public BattleVictorySystem(IBattleResultBuilder resultBuilder)
        {
            this.resultBuilder = resultBuilder ?? throw new ArgumentNullException(nameof(resultBuilder));
        }

        public void Initialize(BattleSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            world = session.World ?? throw new InvalidOperationException("BattleVictorySystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleVictorySystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<TeamComponent>()
                .With<FactionComponent>()
                .With<HealthComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            factions = world.GetStash<FactionComponent>();
            dead = world.GetStash<DeadComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
        }

        public void Tick(float deltaTime)
        {
            if (session == null ||
                session.Completion.IsCompleted ||
                world == null ||
                world.IsDisposed ||
                filter == null)
            {
                return;
            }

            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            CountLivingTeams(
                out var attackerAlive,
                out var defenderAlive,
                out var attackerFactionId,
                out var defenderFactionId);

            if (!attackerAlive && !defenderAlive)
            {
                session.Completion.Complete(resultBuilder.Build(session, BattleOutcome.Retreat, WorldIds.None));
                return;
            }

            if (!defenderAlive)
            {
                session.Completion.Complete(resultBuilder.Build(session, BattleOutcome.AttackerVictory, attackerFactionId));
                return;
            }

            if (!attackerAlive)
            {
                session.Completion.Complete(resultBuilder.Build(session, BattleOutcome.DefenderVictory, defenderFactionId));
                return;
            }

            if (TryGetObjectiveWinner(out var objectiveWinner))
            {
                var winnerFactionId = objectiveWinner == BattleTeamType.Attacker
                    ? attackerFactionId
                    : defenderFactionId;
                var outcome = objectiveWinner == BattleTeamType.Attacker
                    ? BattleOutcome.AttackerVictory
                    : BattleOutcome.DefenderVictory;
                session.Completion.Complete(resultBuilder.Build(session, outcome, winnerFactionId));
            }
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            session = null;
            world = null;
            filter = null;
            teams = null;
            factions = null;
            dead = null;
            tickTimer = default;
        }

        private void CountLivingTeams(
            out bool attackerAlive,
            out bool defenderAlive,
            out int attackerFactionId,
            out int defenderFactionId)
        {
            attackerAlive = false;
            defenderAlive = false;
            attackerFactionId = WorldIds.None;
            defenderFactionId = WorldIds.None;

            foreach (var entity in filter)
            {
                if (dead.Has(entity))
                {
                    continue;
                }

                var team = teams.Get(entity).Value;
                var factionId = factions.Get(entity).Value;
                if (team == BattleTeamType.Attacker)
                {
                    attackerAlive = true;
                    if (attackerFactionId == WorldIds.None)
                    {
                        attackerFactionId = factionId;
                    }
                }
                else
                {
                    defenderAlive = true;
                    if (defenderFactionId == WorldIds.None)
                    {
                        defenderFactionId = factionId;
                    }
                }
            }
        }

        private bool TryGetObjectiveWinner(out BattleTeamType winner)
        {
            winner = BattleTeamType.Attacker;
            var states = session.ObjectiveStates;
            if (states == null || states.Length == 0)
            {
                return false;
            }

            var foundControlPoint = false;
            var foundOwner = false;
            for (var stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                var state = states[stateIndex];
                if (state.Type != BattleObjectiveType.ControlPoint)
                {
                    continue;
                }

                foundControlPoint = true;
                if (!state.HasOwner)
                {
                    return false;
                }

                if (!foundOwner)
                {
                    foundOwner = true;
                    winner = state.OwnerTeam;
                    continue;
                }

                if (state.OwnerTeam != winner)
                {
                    return false;
                }
            }

            return foundControlPoint && foundOwner;
        }

    }
}
