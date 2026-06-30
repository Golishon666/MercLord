using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class WeaponSystem : IBattleRuntimeSystem
    {
        private const float HitscanTraceDuration = 0.08f;

        private readonly SpatialHashSystem spatialHashSystem;
        private readonly ConfigDatabase configDatabase;
        private readonly List<Entity> cooldownBuffer = new List<Entity>();
        private readonly List<Entity> requestBuffer = new List<Entity>();
        private readonly List<Entity> meleeCandidateBuffer = new List<Entity>();

        private World world;
        private Filter cooldownFilter;
        private Filter requestFilter;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TeamComponent> teams;
        private Stash<DamageRequestComponent> damageRequests;
        private Stash<ProjectileComponent> projectiles;
        private Stash<TargetComponent> targets;
        private Stash<ParabolicProjectileComponent> parabolicProjectiles;
        private Stash<ExplosionOnImpactComponent> explosions;
        private Stash<ArtilleryWarningComponent> artilleryWarnings;
        private Stash<ArtilleryTargetMarkerComponent> artilleryTargetMarkers;
        private Stash<DriverComponent> drivers;
        private Stash<HitscanTraceComponent> hitscanTraces;
        private Stash<BattleAudioCueComponent> audioCues;
        private BattleTileMap tileMap;
        private HitChanceFormula hitChanceFormula;
        private int battleSeed;

        public WeaponSystem(
            SpatialHashSystem spatialHashSystem,
            ConfigDatabase configDatabase)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

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

            tileMap = TryCreateTileMap(session.Model);
            hitChanceFormula = configDatabase.CombatBalance != null
                ? configDatabase.CombatBalance.HitChanceFormula
                : HitChanceFormula.Default;
            battleSeed = session.Request.Seed;
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
            velocities = world.GetStash<VelocityComponent>();
            teams = world.GetStash<TeamComponent>();
            damageRequests = world.GetStash<DamageRequestComponent>();
            projectiles = world.GetStash<ProjectileComponent>();
            targets = world.GetStash<TargetComponent>();
            parabolicProjectiles = world.GetStash<ParabolicProjectileComponent>();
            explosions = world.GetStash<ExplosionOnImpactComponent>();
            artilleryWarnings = world.GetStash<ArtilleryWarningComponent>();
            artilleryTargetMarkers = world.GetStash<ArtilleryTargetMarkerComponent>();
            drivers = world.GetStash<DriverComponent>();
            hitscanTraces = world.GetStash<HitscanTraceComponent>();
            audioCues = world.GetStash<BattleAudioCueComponent>();
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
            meleeCandidateBuffer.Clear();
            cooldownFilter = null;
            requestFilter = null;
            world = null;
            cooldowns = null;
            attackRequests = null;
            weapons = null;
            positions = null;
            velocities = null;
            teams = null;
            damageRequests = null;
            projectiles = null;
            targets = null;
            parabolicProjectiles = null;
            explosions = null;
            artilleryWarnings = null;
            artilleryTargetMarkers = null;
            drivers = null;
            hitscanTraces = null;
            audioCues = null;
            tileMap = null;
            hitChanceFormula = default;
            battleSeed = 0;
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
                RemoveArtilleryTargetMarker(request.Target);
                return;
            }

            ref var cooldown = ref cooldowns.Get(request.Source);
            cooldown.Value = weapon.Cooldown;
            SpawnAudioCue(sourcePosition.Value, weapon);

            if (weapon.IsProjectile)
            {
                SpawnProjectile(request, weapon, sourcePosition.Value, targetPosition.Value);
                RemoveArtilleryTargetMarker(request.Target);
                return;
            }

            if (IsMeleeWeapon(weapon.Type))
            {
                BattleExplosionDamage.SpawnDamageRequests(
                    world,
                    spatialHashSystem,
                    positions,
                    teams,
                    damageRequests,
                    request.Source,
                    sourcePosition.Value,
                    weapon.DamageType,
                    weapon.Damage,
                    weapon.Range,
                    meleeCandidateBuffer);
                RemoveArtilleryTargetMarker(request.Target);
                return;
            }

            var hit = RollHitscanHit(request, weapon, sourcePosition.Value, targetPosition.Value);
            SpawnHitscanTrace(sourcePosition.Value, targetPosition.Value, hit);
            if (!hit)
            {
                RemoveArtilleryTargetMarker(request.Target);
                return;
            }

            SpawnDamageRequest(
                request.Source,
                request.Target,
                targetPosition.Value,
                weapon.DamageType,
                weapon.Damage);
            RemoveArtilleryTargetMarker(request.Target);
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
                !positions.Has(request.Target) ||
                drivers.Has(request.Source) ||
                drivers.Has(request.Target))
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
            return math.distancesq(sourcePosition.Value, targetPosition.Value) <= weapon.Range * weapon.Range &&
                   HasLineOfFire(weapon, sourcePosition.Value, targetPosition.Value);
        }

        private static bool IsMeleeWeapon(WeaponType weaponType)
        {
            return weaponType == WeaponType.Sword ||
                   weaponType == WeaponType.Shield;
        }

        private bool HasLineOfFire(WeaponStatsComponent weapon, float2 sourcePosition, float2 targetPosition)
        {
            if (tileMap == null ||
                IsMeleeWeapon(weapon.Type) ||
                weapon.UsesParabolicTrajectory)
            {
                return true;
            }

            return BattleLineOfFire.HasClearLine(
                tileMap,
                sourcePosition,
                targetPosition,
                checkLineOfSight: !weapon.IsProjectile,
                checkProjectiles: true);
        }

        private bool RollHitscanHit(
            AttackRequestComponent request,
            WeaponStatsComponent weapon,
            float2 sourcePosition,
            float2 targetPosition)
        {
            var targetVelocity = velocities.Has(request.Target)
                ? velocities.Get(request.Target).Value
                : float2.zero;
            var targetCover = GetCoverAt(targetPosition);
            var hitChance = BattleHitChance.Calculate(
                hitChanceFormula,
                weapon,
                sourcePosition,
                targetPosition,
                targetVelocity,
                targetCover);
            return BattleHitChance.RollHits(
                battleSeed,
                request.Source,
                request.Target,
                weapon,
                sourcePosition,
                targetPosition,
                hitChance);
        }

        private CoverType GetCoverAt(float2 position)
        {
            if (tileMap == null)
            {
                return CoverType.None;
            }

            var x = (int)math.floor(position.x);
            var y = (int)math.floor(position.y);
            return tileMap.IsInside(x, y)
                ? tileMap.GetTile(x, y).Cover
                : CoverType.None;
        }

        private static BattleTileMap TryCreateTileMap(BattleModel model)
        {
            if (model == null ||
                model.Tiles == null ||
                model.Tiles.Length != model.Width * model.Height ||
                model.Width <= 0 ||
                model.Height <= 0)
            {
                return null;
            }

            return new BattleTileMap(model.Width, model.Height, model.Tiles);
        }

        private void SpawnHitscanTrace(float2 start, float2 end, bool hit)
        {
            var traceEntity = world.CreateEntity();
            hitscanTraces.Set(traceEntity, new HitscanTraceComponent
            {
                Start = start,
                End = end,
                Duration = HitscanTraceDuration,
                RemainingTime = HitscanTraceDuration,
                Hit = hit
            });
        }

        private void SpawnAudioCue(float2 position, WeaponStatsComponent weapon)
        {
            var cueEntity = world.CreateEntity();
            audioCues.Set(cueEntity, new BattleAudioCueComponent
            {
                Type = ResolveAudioCueType(weapon),
                Position = position,
                Volume = 1f,
                Pitch = 1f
            });
        }

        private static BattleAudioCueType ResolveAudioCueType(WeaponStatsComponent weapon)
        {
            if (IsMeleeWeapon(weapon.Type))
            {
                return BattleAudioCueType.MeleeSwing;
            }

            if (weapon.UsesParabolicTrajectory && weapon.ExplosionRadius > 0f)
            {
                return BattleAudioCueType.ArtilleryShot;
            }

            return weapon.IsProjectile
                ? BattleAudioCueType.ProjectileShot
                : BattleAudioCueType.HitscanShot;
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

            var flightTime = distance / math.max(0.001f, weapon.ProjectileSpeed);
            if (weapon.ExplosionRadius > 0f)
            {
                SpawnArtilleryWarning(request.Source, targetPosition, weapon.ExplosionRadius, flightTime);
            }

            parabolicProjectiles.Set(projectileEntity, new ParabolicProjectileComponent
            {
                Start = sourcePosition,
                Target = targetPosition,
                FlightTime = flightTime,
                ElapsedTime = 0f,
                ArcHeight = weapon.ParabolicArcHeight
            });
        }

        private void SpawnArtilleryWarning(Entity source, float2 targetPosition, float radius, float duration)
        {
            var warningEntity = world.CreateEntity();
            positions.Set(warningEntity, new PositionComponent { Value = targetPosition });
            artilleryWarnings.Set(warningEntity, new ArtilleryWarningComponent
            {
                Source = source,
                Radius = radius,
                Duration = duration,
                RemainingTime = duration
            });
        }

        private void RemoveArtilleryTargetMarker(Entity target)
        {
            if (world.Has(target) && artilleryTargetMarkers.Has(target))
            {
                world.RemoveEntity(target);
            }
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
