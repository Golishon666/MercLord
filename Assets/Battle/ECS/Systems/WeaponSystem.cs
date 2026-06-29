using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class WeaponSystem : IBattleRuntimeSystem
    {
        private readonly List<Entity> cooldownBuffer = new List<Entity>();
        private readonly List<Entity> requestBuffer = new List<Entity>();

        private World world;
        private Filter cooldownFilter;
        private Filter requestFilter;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<PositionComponent> positions;
        private Stash<DamageRequestComponent> damageRequests;
        private Stash<ProjectileComponent> projectiles;
        private Stash<TargetComponent> targets;
        private Stash<ParabolicProjectileComponent> parabolicProjectiles;
        private Stash<ExplosionOnImpactComponent> explosions;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("WeaponSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("WeaponSystem cannot initialize on a disposed Morpeh world.");
            }

            cooldownFilter = world.Filter
                .With<AttackCooldownComponent>()
                .Without<DeadComponent>()
                .Build();

            requestFilter = world.Filter
                .With<AttackRequestComponent>()
                .Build();

            cooldowns = world.GetStash<AttackCooldownComponent>();
            attackRequests = world.GetStash<AttackRequestComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            positions = world.GetStash<PositionComponent>();
            damageRequests = world.GetStash<DamageRequestComponent>();
            projectiles = world.GetStash<ProjectileComponent>();
            targets = world.GetStash<TargetComponent>();
            parabolicProjectiles = world.GetStash<ParabolicProjectileComponent>();
            explosions = world.GetStash<ExplosionOnImpactComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed)
            {
                return;
            }

            TickCooldowns(deltaTime);
            ProcessRequests();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                cooldownFilter?.Dispose();
                requestFilter?.Dispose();
            }

            cooldownBuffer.Clear();
            requestBuffer.Clear();
            cooldownFilter = null;
            requestFilter = null;
            world = null;
            cooldowns = null;
            attackRequests = null;
            weapons = null;
            positions = null;
            damageRequests = null;
            projectiles = null;
            targets = null;
            parabolicProjectiles = null;
            explosions = null;
        }

        private void TickCooldowns(float deltaTime)
        {
            cooldownBuffer.Clear();
            foreach (var entity in cooldownFilter)
            {
                cooldownBuffer.Add(entity);
            }

            for (var entityIndex = 0; entityIndex < cooldownBuffer.Count; entityIndex++)
            {
                ref var cooldown = ref cooldowns.Get(cooldownBuffer[entityIndex]);
                cooldown.Value = math.max(0f, cooldown.Value - deltaTime);
            }

            cooldownBuffer.Clear();
        }

        private void ProcessRequests()
        {
            requestBuffer.Clear();
            foreach (var entity in requestFilter)
            {
                requestBuffer.Add(entity);
            }

            for (var requestIndex = 0; requestIndex < requestBuffer.Count; requestIndex++)
            {
                var requestEntity = requestBuffer[requestIndex];
                var request = attackRequests.Get(requestEntity);
                ProcessRequest(request);
                world.RemoveEntity(requestEntity);
            }

            requestBuffer.Clear();
        }

        private void ProcessRequest(AttackRequestComponent request)
        {
            if (!CanAttack(request, out var weapon, out var sourcePosition, out var targetPosition))
            {
                return;
            }

            ref var cooldown = ref cooldowns.Get(request.Source);
            cooldown.Value = weapon.Cooldown;

            if (weapon.IsProjectile)
            {
                SpawnProjectile(request, weapon, sourcePosition.Value, targetPosition.Value);
                return;
            }

            SpawnDamageRequest(
                request.Source,
                request.Target,
                targetPosition.Value,
                weapon.DamageType,
                weapon.Damage);
        }

        private bool CanAttack(
            AttackRequestComponent request,
            out WeaponStatsComponent weapon,
            out PositionComponent sourcePosition,
            out PositionComponent targetPosition)
        {
            weapon = default;
            sourcePosition = default;
            targetPosition = default;

            if (!world.Has(request.Source) ||
                !world.Has(request.Target) ||
                !weapons.Has(request.Source) ||
                !cooldowns.Has(request.Source) ||
                !positions.Has(request.Source) ||
                !positions.Has(request.Target))
            {
                return false;
            }

            weapon = weapons.Get(request.Source);
            if (weapon.WeaponConfigId != request.WeaponConfigId)
            {
                return false;
            }

            var cooldown = cooldowns.Get(request.Source);
            if (cooldown.Value > 0f)
            {
                return false;
            }

            sourcePosition = positions.Get(request.Source);
            targetPosition = positions.Get(request.Target);
            return math.distancesq(sourcePosition.Value, targetPosition.Value) <= weapon.Range * weapon.Range;
        }

        private void SpawnProjectile(
            AttackRequestComponent request,
            WeaponStatsComponent weapon,
            float2 sourcePosition,
            float2 targetPosition)
        {
            var projectileEntity = world.CreateEntity();
            positions.Set(projectileEntity, new PositionComponent { Value = sourcePosition });
            targets.Set(projectileEntity, new TargetComponent { Target = request.Target });
            projectiles.Set(projectileEntity, new ProjectileComponent
            {
                Source = request.Source,
                Damage = weapon.Damage,
                DamageType = weapon.DamageType,
                Speed = weapon.ProjectileSpeed
            });

            if (weapon.ExplosionRadius > 0f)
            {
                explosions.Set(projectileEntity, new ExplosionOnImpactComponent
                {
                    Radius = weapon.ExplosionRadius
                });
            }

            if (!weapon.UsesParabolicTrajectory)
            {
                return;
            }

            var distance = math.distance(sourcePosition, targetPosition);
            if (distance <= float.Epsilon)
            {
                return;
            }

            parabolicProjectiles.Set(projectileEntity, new ParabolicProjectileComponent
            {
                Start = sourcePosition,
                Target = targetPosition,
                FlightTime = distance / weapon.ProjectileSpeed,
                ElapsedTime = 0f,
                ArcHeight = weapon.ParabolicArcHeight
            });
        }

        private void SpawnDamageRequest(
            Entity source,
            Entity target,
            float2 hitPosition,
            MercLord.Game.Configs.DamageType damageType,
            int amount)
        {
            var damageRequestEntity = world.CreateEntity();
            damageRequests.Set(damageRequestEntity, new DamageRequestComponent
            {
                Source = source,
                Target = target,
                HitPosition = hitPosition,
                DamageType = damageType,
                Amount = amount
            });
        }
    }
}
