using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.UI
{
    public sealed class BattleMinimapHudPresenter : IDisposable
    {
        private readonly List<BattleMinimapHudPin> pins = new List<BattleMinimapHudPin>(1024);
        private readonly List<BattleMinimapHudDangerZone> dangerZones = new List<BattleMinimapHudDangerZone>(32);

        private BattleSession session;
        private World world;
        private Filter unitFilter;
        private Filter playerFilter;
        private Filter warningFilter;
        private Stash<TeamComponent> teams;
        private Stash<PositionComponent> positions;
        private Stash<HealthComponent> health;
        private Stash<DeadComponent> dead;
        private Stash<DriverComponent> drivers;
        private Stash<PlayerControlledComponent> playerControlled;
        private Stash<ArtilleryWarningComponent> artilleryWarnings;

        public void Bind(BattleSession session)
        {
            DisposeFilters();

            this.session = session ?? throw new ArgumentNullException(nameof(session));
            world = session.World ?? throw new InvalidOperationException("BattleMinimapHudPresenter requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleMinimapHudPresenter cannot bind to a disposed Morpeh world.");
            }

            unitFilter = world.Filter
                .With<TeamComponent>()
                .With<PositionComponent>()
                .With<HealthComponent>()
                .Build();
            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .With<PositionComponent>()
                .Without<DeadComponent>()
                .Without<DriverComponent>()
                .Build();
            warningFilter = world.Filter
                .With<PositionComponent>()
                .With<ArtilleryWarningComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            positions = world.GetStash<PositionComponent>();
            health = world.GetStash<HealthComponent>();
            dead = world.GetStash<DeadComponent>();
            drivers = world.GetStash<DriverComponent>();
            playerControlled = world.GetStash<PlayerControlledComponent>();
            artilleryWarnings = world.GetStash<ArtilleryWarningComponent>();
        }

        public BattleMinimapHudSnapshot BuildSnapshot()
        {
            if (session == null ||
                session.Model == null ||
                session.Model.Width <= 0 ||
                session.Model.Height <= 0 ||
                world == null ||
                world.IsDisposed ||
                unitFilter == null)
            {
                return new BattleMinimapHudSnapshot(
                    false,
                    0,
                    0,
                    Array.Empty<BattleMinimapHudPin>(),
                    Array.Empty<BattleMinimapHudDangerZone>(),
                    0,
                    false,
                    BattleTeamType.Attacker,
                    0f,
                    false,
                    false,
                    BattleTeamType.Attacker,
                    0,
                    0,
                    false,
                    BattleOutcome.None);
            }

            pins.Clear();
            dangerZones.Clear();
            var attackerAlive = 0;
            var defenderAlive = 0;
            var hasPlayer = TryGetPlayerTeam(out var playerTeam);
            var hasObjectiveState = TryGetPrimaryObjectiveState(out var objectiveState);

            foreach (var entity in unitFilter)
            {
                if (!IsAlive(entity))
                {
                    continue;
                }

                var team = teams.Get(entity).Value;
                if (team == BattleTeamType.Attacker)
                {
                    attackerAlive++;
                }
                else
                {
                    defenderAlive++;
                }

                pins.Add(new BattleMinimapHudPin(
                    positions.Get(entity).Value,
                    team,
                    playerControlled.Has(entity)));
            }

            BuildDangerZones();

            return new BattleMinimapHudSnapshot(
                true,
                session.Model.Width,
                session.Model.Height,
                pins,
                dangerZones,
                session.Model.Objectives?.Length ?? 0,
                hasObjectiveState && objectiveState.HasCaptureTeam,
                hasObjectiveState ? objectiveState.CaptureTeam : BattleTeamType.Attacker,
                hasObjectiveState ? objectiveState.CaptureProgress : 0f,
                hasObjectiveState && objectiveState.IsContested,
                hasPlayer,
                playerTeam,
                attackerAlive,
                defenderAlive,
                session.Completion.IsCompleted,
                session.Completion.Result?.Outcome ?? BattleOutcome.None);
        }

        public void Dispose()
        {
            DisposeFilters();
            pins.Clear();
            dangerZones.Clear();
            session = null;
            world = null;
            teams = null;
            positions = null;
            health = null;
            dead = null;
            drivers = null;
            playerControlled = null;
            artilleryWarnings = null;
        }

        private bool TryGetPlayerTeam(out BattleTeamType playerTeam)
        {
            playerTeam = BattleTeamType.Attacker;
            if (playerFilter == null)
            {
                return false;
            }

            foreach (var entity in playerFilter)
            {
                playerTeam = teams.Get(entity).Value;
                return true;
            }

            return false;
        }

        private bool TryGetPrimaryObjectiveState(out BattleObjectiveRuntimeState objectiveState)
        {
            objectiveState = null;
            var states = session?.ObjectiveStates;
            if (states == null || states.Length == 0)
            {
                return false;
            }

            for (var stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                var state = states[stateIndex];
                if (state == null || state.Type != BattleObjectiveType.ControlPoint)
                {
                    continue;
                }

                if (objectiveState == null || state.Priority > objectiveState.Priority)
                {
                    objectiveState = state;
                }
            }

            return objectiveState != null;
        }

        private void BuildDangerZones()
        {
            if (warningFilter == null)
            {
                return;
            }

            foreach (var entity in warningFilter)
            {
                var warning = artilleryWarnings.Get(entity);
                var remainingFraction = warning.Duration > 0f
                    ? warning.RemainingTime / warning.Duration
                    : 0f;
                dangerZones.Add(new BattleMinimapHudDangerZone(
                    positions.Get(entity).Value,
                    warning.Radius,
                    remainingFraction));
            }
        }

        private bool IsAlive(Entity entity)
        {
            if (dead.Has(entity) || drivers.Has(entity))
            {
                return false;
            }

            return health.Get(entity).Current > 0;
        }

        private void DisposeFilters()
        {
            if (world != null && !world.IsDisposed)
            {
                unitFilter?.Dispose();
                playerFilter?.Dispose();
                warningFilter?.Dispose();
            }

            unitFilter = null;
            playerFilter = null;
            warningFilter = null;
        }
    }
}
