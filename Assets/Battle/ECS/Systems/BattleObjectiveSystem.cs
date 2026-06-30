using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class BattleObjectiveSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.25f;
        private const float CaptureDuration = 8f;

        private BattleSession session;
        private World world;
        private Filter unitFilter;
        private Stash<TeamComponent> teams;
        private Stash<PositionComponent> positions;
        private Stash<HealthComponent> health;
        private Stash<DeadComponent> dead;
        private Stash<DriverComponent> drivers;
        private BattleCadenceTimer tickTimer;

        public void Initialize(BattleSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            world = session.World ?? throw new InvalidOperationException("BattleObjectiveSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleObjectiveSystem cannot initialize on a disposed Morpeh world.");
            }

            unitFilter = world.Filter
                .With<TeamComponent>()
                .With<PositionComponent>()
                .With<HealthComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            positions = world.GetStash<PositionComponent>();
            health = world.GetStash<HealthComponent>();
            dead = world.GetStash<DeadComponent>();
            drivers = world.GetStash<DriverComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
        }

        public void Tick(float deltaTime)
        {
            if (session == null ||
                session.Completion.IsCompleted ||
                world == null ||
                world.IsDisposed ||
                unitFilter == null ||
                session.ObjectiveStates.Length == 0)
            {
                return;
            }

            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            var objectives = session.Model.Objectives ?? Array.Empty<BattleObjectiveZone>();
            var effectiveDeltaTime = Mathf.Max(0f, deltaTime);
            var objectiveCount = Mathf.Min(objectives.Length, session.ObjectiveStates.Length);
            for (var objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
            {
                UpdateObjective(objectives[objectiveIndex], session.ObjectiveStates[objectiveIndex], effectiveDeltaTime);
            }
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && unitFilter != null)
            {
                unitFilter.Dispose();
            }

            session = null;
            world = null;
            unitFilter = null;
            teams = null;
            positions = null;
            health = null;
            dead = null;
            drivers = null;
            tickTimer = default;
        }

        private void UpdateObjective(
            BattleObjectiveZone objective,
            BattleObjectiveRuntimeState state,
            float deltaTime)
        {
            CountPresence(objective, out var attackerPresence, out var defenderPresence);
            state.AttackerPresence = attackerPresence;
            state.DefenderPresence = defenderPresence;
            state.IsContested = attackerPresence > 0 && defenderPresence > 0;

            if (state.IsContested)
            {
                return;
            }

            if (attackerPresence <= 0 && defenderPresence <= 0)
            {
                HoldOrDecayCapture(state, deltaTime);
                return;
            }

            var captureTeam = attackerPresence > 0
                ? BattleTeamType.Attacker
                : BattleTeamType.Defender;
            AdvanceCapture(state, captureTeam, deltaTime);
        }

        private void CountPresence(
            BattleObjectiveZone objective,
            out int attackerPresence,
            out int defenderPresence)
        {
            attackerPresence = 0;
            defenderPresence = 0;

            foreach (var entity in unitFilter)
            {
                if (dead.Has(entity) || drivers.Has(entity) || health.Get(entity).Current <= 0)
                {
                    continue;
                }

                var position = positions.Get(entity).Value;
                if (!objective.Contains(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y)))
                {
                    continue;
                }

                if (teams.Get(entity).Value == BattleTeamType.Attacker)
                {
                    attackerPresence++;
                }
                else
                {
                    defenderPresence++;
                }
            }
        }

        private static void AdvanceCapture(
            BattleObjectiveRuntimeState state,
            BattleTeamType captureTeam,
            float deltaTime)
        {
            if (state.HasOwner && state.OwnerTeam == captureTeam)
            {
                state.HasCaptureTeam = true;
                state.CaptureTeam = captureTeam;
                state.CaptureProgress = 1f;
                return;
            }

            if (!state.HasCaptureTeam || state.CaptureTeam != captureTeam)
            {
                state.HasCaptureTeam = true;
                state.CaptureTeam = captureTeam;
                state.CaptureProgress = 0f;
            }

            state.CaptureProgress = Mathf.Clamp01(state.CaptureProgress + deltaTime / CaptureDuration);
            if (state.CaptureProgress < 1f)
            {
                return;
            }

            state.HasOwner = true;
            state.OwnerTeam = captureTeam;
        }

        private static void HoldOrDecayCapture(BattleObjectiveRuntimeState state, float deltaTime)
        {
            if (state.HasOwner)
            {
                state.HasCaptureTeam = true;
                state.CaptureTeam = state.OwnerTeam;
                state.CaptureProgress = 1f;
                return;
            }

            state.CaptureProgress = Mathf.Max(0f, state.CaptureProgress - deltaTime / CaptureDuration);
            if (state.CaptureProgress <= 0f)
            {
                state.HasCaptureTeam = false;
            }
        }
    }
}
