using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ParabolicProjectileSystem : IBattleRuntimeSystem
    {
        private readonly List<Entity> projectileBuffer = new List<Entity>();
        private readonly List<Entity> explosionCandidateBuffer = new List<Entity>();
        private readonly SpatialHashSystem spatialHashSystem;

        private World world;
        private Filter filter;
        private Stash<ProjectileComponent> projectiles;
        private Stash<ParabolicProjectileComponent> parabolicProjectiles;
        private Stash<PositionComponent> positions;
        private Stash<TeamComponent> teams;
        private Stash<TargetComponent> targets;
        private Stash<DamageRequestComponent> damageRequests;
        private Stash<ExplosionOnImpactComponent> explosions;
        private Stash<BattleCameraShakeComponent> cameraShakes;

        public ParabolicProjectileSystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("ParabolicProjectileSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ParabolicProjectileSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<ProjectileComponent>()
                .With<ParabolicProjectileComponent>()
                .With<PositionComponent>()
                .With<TargetComponent>()
                .Build();

            projectiles = world.GetStash<ProjectileComponent>();
            parabolicProjectiles = world.GetStash<ParabolicProjectileComponent>();
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
            parabolicProjectiles = null;
            positions = null;
            teams = null;
            targets = null;
            damageRequests = null;
            explosions = null;
            cameraShakes = null;
        }

        private void TickProjectile(Entity projectileEntity, float deltaTime)
        {
            ref var parabolic = ref parabolicProjectiles.Get(projectileEntity);
            ref var position = ref positions.Get(projectileEntity);
            parabolic.ElapsedTime += deltaTime;

            var normalizedTime = math.saturate(parabolic.ElapsedTime / parabolic.FlightTime);
            var basePosition = math.lerp(parabolic.Start, parabolic.Target, normalizedTime);
            var arcOffset = math.sin(normalizedTime * math.PI) * parabolic.ArcHeight;
            position.Value = basePosition + new float2(0f, arcOffset);

            if (normalizedTime < 1f)
            {
                return;
            }

            var target = targets.Get(projectileEntity).Target;
            var projectile = projectiles.Get(projectileEntity);
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
                    parabolic.Target,
                    projectile.DamageType,
                    projectile.Damage,
                    explosion.Radius,
                    explosionCandidateBuffer);
                SpawnCameraShake(parabolic.Target, explosion.Radius);
            }
            else if (world.Has(target))
            {
                SpawnDamageRequest(projectile, target, parabolic.Target);
            }

            world.RemoveEntity(projectileEntity);
        }

        private void SpawnDamageRequest(
            ProjectileComponent projectile,
            Entity target,
            float2 hitPosition)
        {
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
