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

        private World world;
        private Filter filter;
        private Stash<ProjectileComponent> projectiles;
        private Stash<PositionComponent> positions;
        private Stash<TargetComponent> targets;
        private Stash<DamageRequestComponent> damageRequests;

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
            targets = world.GetStash<TargetComponent>();
            damageRequests = world.GetStash<DamageRequestComponent>();
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
            filter = null;
            world = null;
            projectiles = null;
            positions = null;
            targets = null;
            damageRequests = null;
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
                SpawnDamageRequest(projectile, target, targetPosition.Value);
                world.RemoveEntity(projectileEntity);
                return;
            }

            position.Value += math.normalizesafe(toTarget) * step;
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
    }
}
