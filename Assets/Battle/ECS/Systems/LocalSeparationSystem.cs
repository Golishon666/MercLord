using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class LocalSeparationSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.05f;
        private const float SeparationRadius = 0.7f;
        private const float SeparationWeight = 0.45f;
        private const float MinimumDistanceSquared = 0.0001f;
        private const float SteeringDeadZoneSquared = 0.0001f;
        private const float MaximumVelocitySquared = 1f;

        private readonly SpatialHashSystem spatialHashSystem;
        private readonly List<Entity> entityBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<DeadComponent> dead;
        private BattleCadenceTimer tickTimer;

        public LocalSeparationSystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("LocalSeparationSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("LocalSeparationSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<MovementStatsComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Without<PlayerControlledComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            dead = world.GetStash<DeadComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            entityBuffer.Clear();
            foreach (var entity in filter)
            {
                entityBuffer.Add(entity);
            }

            for (var entityIndex = 0; entityIndex < entityBuffer.Count; entityIndex++)
            {
                ApplySeparation(entityBuffer[entityIndex]);
            }

            entityBuffer.Clear();
            candidateBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            entityBuffer.Clear();
            candidateBuffer.Clear();
            filter = null;
            world = null;
            positions = null;
            velocities = null;
            dead = null;
            tickTimer = default;
        }

        private void ApplySeparation(Entity entity)
        {
            var position = positions.Get(entity).Value;
            var steering = float2.zero;
            spatialHashSystem.GetEntitiesInRange(position, SeparationRadius, candidateBuffer);

            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (candidate.Equals(entity) ||
                    !world.Has(candidate) ||
                    !positions.Has(candidate) ||
                    dead.Has(candidate))
                {
                    continue;
                }

                var offset = position - positions.Get(candidate).Value;
                var distanceSquared = math.lengthsq(offset);
                if (distanceSquared >= SeparationRadius * SeparationRadius)
                {
                    continue;
                }

                var distance = distanceSquared > MinimumDistanceSquared
                    ? math.sqrt(distanceSquared)
                    : 0f;
                var direction = distanceSquared > MinimumDistanceSquared
                    ? offset / distance
                    : GetOverlapDirection(entity, candidate);
                var strength = (SeparationRadius - distance) / SeparationRadius;
                steering += direction * strength;
            }

            if (math.lengthsq(steering) <= SteeringDeadZoneSquared)
            {
                return;
            }

            ref var velocity = ref velocities.Get(entity);
            var desired = velocity.Value + steering * SeparationWeight;
            if (math.lengthsq(desired) > MaximumVelocitySquared)
            {
                desired = math.normalizesafe(desired);
            }

            velocity.Value = desired;
        }

        private static float2 GetOverlapDirection(Entity entity, Entity candidate)
        {
            unchecked
            {
                var hash = (uint)(entity.GetHashCode() * 397) ^ (uint)candidate.GetHashCode();
                var angle = (hash % 360u) * (math.PI / 180f);
                return new float2(math.cos(angle), math.sin(angle));
            }
        }
    }
}
