using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class TargetSearchSystem : IBattleRuntimeSystem
    {
        private readonly SpatialHashSystem spatialHashSystem;
        private readonly List<Entity> actorBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<TeamComponent> teams;
        private Stash<AIStatsComponent> aiStats;
        private Stash<AIThinkTimerComponent> timers;
        private Stash<TargetComponent> targets;
        private Stash<DriverComponent> drivers;

        public TargetSearchSystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("TargetSearchSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("TargetSearchSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<TeamComponent>()
                .With<AIStatsComponent>()
                .With<AIThinkTimerComponent>()
                .Without<DeadComponent>()
                .Without<DriverComponent>()
                .Without<PlayerControlledComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            teams = world.GetStash<TeamComponent>();
            aiStats = world.GetStash<AIStatsComponent>();
            timers = world.GetStash<AIThinkTimerComponent>();
            targets = world.GetStash<TargetComponent>();
            drivers = world.GetStash<DriverComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            actorBuffer.Clear();
            foreach (var entity in filter)
            {
                actorBuffer.Add(entity);
            }

            for (var actorIndex = 0; actorIndex < actorBuffer.Count; actorIndex++)
            {
                SearchIfReady(actorBuffer[actorIndex], deltaTime);
            }

            actorBuffer.Clear();
            candidateBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            actorBuffer.Clear();
            candidateBuffer.Clear();
            filter = null;
            world = null;
            positions = null;
            teams = null;
            aiStats = null;
            timers = null;
            targets = null;
            drivers = null;
        }

        private void SearchIfReady(Entity actor, float deltaTime)
        {
            ref var timer = ref timers.Get(actor);
            timer.TimeUntilNextThink -= deltaTime;
            if (timer.TimeUntilNextThink > 0f)
            {
                return;
            }

            var ai = aiStats.Get(actor);
            timer.TimeUntilNextThink += ai.ThinkInterval;

            var position = positions.Get(actor);
            var team = teams.Get(actor);
            spatialHashSystem.GetOpponentsInRange(
                position.Value,
                ai.TargetSearchRadius,
                team.Value,
                candidateBuffer);

            var bestTarget = default(Entity);
            var bestDistance = float.MaxValue;
            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (candidate.Equals(actor) || !positions.Has(candidate))
                {
                    continue;
                }

                if (drivers.Has(candidate))
                {
                    continue;
                }

                var candidatePosition = positions.Get(candidate);
                var distance = math.distancesq(position.Value, candidatePosition.Value);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                }
            }

            if (world.Has(bestTarget))
            {
                targets.Set(actor, new TargetComponent { Target = bestTarget });
            }
            else if (targets.Has(actor))
            {
                targets.Remove(actor);
            }
        }
    }
}
