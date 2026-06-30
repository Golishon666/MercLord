using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class DecisionSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.2f;

        private readonly List<Entity> actorBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();
        private readonly List<Entity> clusterBuffer = new List<Entity>();

        private readonly SpatialHashSystem spatialHashSystem;

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TargetComponent> targets;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AIStatsComponent> aiStats;
        private Stash<TeamComponent> teams;
        private Stash<HealthComponent> healths;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<DriverComponent> drivers;
        private BattleCadenceTimer tickTimer;

        public DecisionSystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("DecisionSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("DecisionSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<TargetComponent>()
                .With<WeaponStatsComponent>()
                .With<AttackCooldownComponent>()
                .With<AIStatsComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Without<DriverComponent>()
                .Without<PlayerControlledComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            targets = world.GetStash<TargetComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            cooldowns = world.GetStash<AttackCooldownComponent>();
            aiStats = world.GetStash<AIStatsComponent>();
            teams = world.GetStash<TeamComponent>();
            healths = world.GetStash<HealthComponent>();
            attackRequests = world.GetStash<AttackRequestComponent>();
            drivers = world.GetStash<DriverComponent>();
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

            actorBuffer.Clear();
            foreach (var entity in filter)
            {
                actorBuffer.Add(entity);
            }

            for (var actorIndex = 0; actorIndex < actorBuffer.Count; actorIndex++)
            {
                Decide(actorBuffer[actorIndex]);
            }

            actorBuffer.Clear();
            candidateBuffer.Clear();
            clusterBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            actorBuffer.Clear();
            candidateBuffer.Clear();
            clusterBuffer.Clear();
            filter = null;
            world = null;
            positions = null;
            velocities = null;
            targets = null;
            weapons = null;
            cooldowns = null;
            aiStats = null;
            teams = null;
            healths = null;
            attackRequests = null;
            drivers = null;
            tickTimer = default;
        }

        private void Decide(Entity actor)
        {
            var ai = aiStats.Get(actor);
            if (ai.Type == AIType.Passive)
            {
                velocities.Get(actor).Value = float2.zero;
                return;
            }

            var target = targets.Get(actor).Target;
            if (!IsValidTarget(target))
            {
                targets.Remove(actor);
                velocities.Get(actor).Value = float2.zero;
                return;
            }

            ref var velocity = ref velocities.Get(actor);
            var actorPosition = positions.Get(actor);
            var targetPosition = positions.Get(target);
            var toTarget = targetPosition.Value - actorPosition.Value;
            var distanceSquared = math.lengthsq(toTarget);
            var direction = math.normalizesafe(toTarget);
            var weapon = weapons.Get(actor);

            if (ShouldRetreat(actor, ai))
            {
                velocity.Value = -direction;
                return;
            }

            var preferredDistanceSquared = ai.PreferredAttackDistance * ai.PreferredAttackDistance;
            velocity.Value = distanceSquared > preferredDistanceSquared
                ? direction
                : float2.zero;

            var cooldown = cooldowns.Get(actor);
            if (cooldown.Value > 0f)
            {
                return;
            }

            var requestTarget = target;
            var requestTargetPosition = targetPosition.Value;
            if (ArtilleryClusterTargeting.TryFindBestCluster(
                    world,
                    spatialHashSystem,
                    positions,
                    healths,
                    actorPosition.Value,
                    teams.Get(actor).Value,
                    weapon,
                    candidateBuffer,
                    clusterBuffer,
                    out var clusterTargetPosition))
            {
                requestTarget = ArtilleryClusterTargeting.CreateTargetMarker(world, positions, clusterTargetPosition);
                requestTargetPosition = clusterTargetPosition;
            }

            var weaponRangeSquared = weapon.Range * weapon.Range;
            if (math.distancesq(actorPosition.Value, requestTargetPosition) <= weaponRangeSquared)
            {
                attackRequests.Set(world.CreateEntity(), new AttackRequestComponent
                {
                    Source = actor,
                    Target = requestTarget,
                    WeaponConfigId = weapon.WeaponConfigId
                });
            }
        }

        private bool IsValidTarget(Entity target)
        {
            return world.Has(target) &&
                   positions.Has(target) &&
                   healths.Has(target) &&
                   !drivers.Has(target);
        }

        private bool ShouldRetreat(Entity actor, AIStatsComponent ai)
        {
            if (ai.RetreatHealthPercent <= 0f || !healths.Has(actor))
            {
                return false;
            }

            var health = healths.Get(actor);
            return health.Max > 0 &&
                   (float)health.Current / health.Max <= ai.RetreatHealthPercent;
        }
    }
}
