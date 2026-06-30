using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ProjectileSystem : IBattleRuntimeSystem
    {
        private readonly List<Entity> projectileBuffer = new List<Entity>();
        private readonly List<Entity> explosionCandidateBuffer = new List<Entity>();
        private readonly SpatialHashSystem spatialHashSystem;

        private World world;
        private Filter filter;
        private Stash<ProjectileComponent> projectiles;
        private Stash<PositionComponent> positions;
        private Stash<TeamComponent> teams;
        private Stash<TargetComponent> targets;
        private Stash<DamageRequestComponent> damageRequests;
        private Stash<ExplosionOnImpactComponent> explosions;
        private Stash<BattleCameraShakeComponent> cameraShakes;

        public ProjectileSystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("ProjectileSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ProjectileSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<TargetComponent>()
                .Without<ParabolicProjectileComponent>()
                .Build();

            projectiles = world.GetStash<ProjectileComponent>();
            positions = world.GetStash<PositionComponent>();
            teams = world.GetStash<TeamComponent>();
            targets = world.GetStash<TargetComponent>();
            damageRequests = world.GetStash<DamageRequestComponent>();
            explosions = world.GetStash<ExplosionOnImpactComponent>();
            cameraShakes = world.GetStash<BattleCameraShakeComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            projectileBuffer.Clear();
            foreach (var entity in filter)
            {
                projectileBuffer.Add(entity);
            }

            for (var projectileIndex = 0; projectileIndex < projectileBuffer.Count; projectileIndex++)
            {
                TickProjectile(projectileBuffer[projectileIndex], deltaTime);
            }

            projectileBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            projectileBuffer.Clear();
            explosionCandidateBuffer.Clear();
            filter = null;
            world = null;
            projectiles = null;
            positions = null;
            teams = null;
            targets = null;
            damageRequests = null;
            explosions = null;
            cameraShakes = null;
        }

        private void TickProjectile(Entity projectileEntity, float deltaTime)
        {
            var projectile = projectiles.Get(projectileEntity);
            var target = targets.Get(projectileEntity).Target;
            if (!world.Has(target) || !positions.Has(target))
            {
                world.RemoveEntity(projectileEntity);
                return;
            }

            ref var position = ref positions.Get(projectileEntity);
            var targetPosition = positions.Get(target);
            var toTarget = targetPosition.Value - position.Value;
            var distance = math.length(toTarget);
            var step = projectile.Speed * deltaTime;

            if (distance <= step || distance <= float.Epsilon)
            {
                position.Value = targetPosition.Value;
                SpawnDamageRequests(projectileEntity, projectile, target, targetPosition.Value);
                world.RemoveEntity(projectileEntity);
                return;
            }

            position.Value += math.normalizesafe(toTarget) * step;
        }

        private void SpawnDamageRequests(
            Entity projectileEntity,
            ProjectileComponent projectile,
            Entity target,
            float2 hitPosition)
        {
            if (explosions.Has(projectileEntity))
            {
                var explosion = explosions.Get(projectileEntity);
                BattleExplosionDamage.SpawnDamageRequests(
                    world,
                    spatialHashSystem,
                    positions,
                    teams,
                    damageRequests,
                    projectile.Source,
                    hitPosition,
                    projectile.DamageType,
                    projectile.Damage,
                    explosion.Radius,
                    explosionCandidateBuffer);
                SpawnCameraShake(hitPosition, explosion.Radius);
                return;
            }

            var damageRequestEntity = world.CreateEntity();
            damageRequests.Set(damageRequestEntity, new DamageRequestComponent
            {
                Source = projectile.Source,
                Target = target,
                HitPosition = hitPosition,
                DamageType = projectile.DamageType,
                Amount = projectile.Damage
            });
        }

        private void SpawnCameraShake(float2 hitPosition, float radius)
        {
            var duration = math.clamp(0.12f + radius * 0.04f, 0.12f, 0.45f);
            var shakeEntity = world.CreateEntity();
            cameraShakes.Set(shakeEntity, new BattleCameraShakeComponent
            {
                Position = hitPosition,
                Intensity = math.clamp(radius * 0.035f, 0.04f, 0.25f),
                Duration = duration,
                RemainingTime = duration
            });
        }
    }
}
