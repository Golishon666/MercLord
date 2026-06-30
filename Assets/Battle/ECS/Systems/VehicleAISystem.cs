using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Pathfinding;
using MercLord.Battle.Tiles;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class VehicleAISystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.2f;
        private const float SearchRangeMultiplier = 1.5f;
        private const float PreferredRangeMultiplier = 0.75f;

        private readonly SpatialHashSystem spatialHashSystem;
        private readonly BattleFlowFieldBuilder flowFieldBuilder = new BattleFlowFieldBuilder();
        private readonly List<Entity> vehicleBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();
        private readonly List<Entity> clusterBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<VehicleComponent> vehicles;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TeamComponent> teams;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<TargetComponent> targets;
        private Stash<HealthComponent> healths;
        private BattleCadenceTimer tickTimer;
        private BattleTileMap tileMap;

        public VehicleAISystem(SpatialHashSystem spatialHashSystem)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("VehicleAISystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("VehicleAISystem cannot initialize on a disposed Morpeh world.");
            }

            tileMap = TryCreateTileMap(session.Model);
            filter = world.Filter
                .With<VehicleComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<TeamComponent>()
                .With<WeaponStatsComponent>()
                .With<AttackCooldownComponent>()
                .Without<DeadComponent>()
                .Build();

            vehicles = world.GetStash<VehicleComponent>();
            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            teams = world.GetStash<TeamComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            cooldowns = world.GetStash<AttackCooldownComponent>();
            attackRequests = world.GetStash<AttackRequestComponent>();
            targets = world.GetStash<TargetComponent>();
            healths = world.GetStash<HealthComponent>();
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

            vehicleBuffer.Clear();
            foreach (var entity in filter)
            {
                vehicleBuffer.Add(entity);
            }

            for (var vehicleIndex = 0; vehicleIndex < vehicleBuffer.Count; vehicleIndex++)
            {
                TickVehicle(vehicleBuffer[vehicleIndex]);
            }

            vehicleBuffer.Clear();
            candidateBuffer.Clear();
            clusterBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            vehicleBuffer.Clear();
            candidateBuffer.Clear();
            clusterBuffer.Clear();
            filter = null;
            world = null;
            vehicles = null;
            positions = null;
            velocities = null;
            teams = null;
            weapons = null;
            cooldowns = null;
            attackRequests = null;
            targets = null;
            healths = null;
            tickTimer = default;
            tileMap = null;
        }

        private void TickVehicle(Entity vehicleEntity)
        {
            ref var vehicle = ref vehicles.Get(vehicleEntity);
            if (vehicle.State != VehicleStateType.AIControlled)
            {
                return;
            }

            var position = positions.Get(vehicleEntity).Value;
            var team = teams.Get(vehicleEntity).Value;
            var weapon = weapons.Get(vehicleEntity);
            var searchRange = math.max(0f, weapon.Range * SearchRangeMultiplier);
            var canFire = cooldowns.Get(vehicleEntity).Value <= 0f;
            if (canFire &&
                ArtilleryClusterTargeting.TryFindBestCluster(
                    world,
                    spatialHashSystem,
                    positions,
                    healths,
                    position,
                    team,
                    weapon,
                    candidateBuffer,
                    clusterBuffer,
                    out var clusterTargetPosition))
            {
                var clusterTarget = ArtilleryClusterTargeting.CreateTargetMarker(world, positions, clusterTargetPosition);
                targets.Set(vehicleEntity, new TargetComponent { Target = clusterTarget });
                ApplyVehicleMovement(vehicleEntity, position, clusterTargetPosition, weapon);
                attackRequests.Set(world.CreateEntity(), new AttackRequestComponent
                {
                    Source = vehicleEntity,
                    Target = clusterTarget,
                    WeaponConfigId = weapon.WeaponConfigId
                });
                return;
            }

            if (searchRange <= 0f ||
                !TryFindNearestTarget(vehicleEntity, position, team, searchRange, out var target, out var targetPosition))
            {
                velocities.Get(vehicleEntity).Value = float2.zero;
                if (targets.Has(vehicleEntity))
                {
                    targets.Remove(vehicleEntity);
                }

                return;
            }

            targets.Set(vehicleEntity, new TargetComponent { Target = target });
            ApplyVehicleMovement(vehicleEntity, position, targetPosition, weapon);
            if (!canFire ||
                math.distancesq(position, targetPosition) > weapon.Range * weapon.Range)
            {
                return;
            }

            var requestEntity = world.CreateEntity();
            attackRequests.Set(requestEntity, new AttackRequestComponent
            {
                Source = vehicleEntity,
                Target = target,
                WeaponConfigId = weapon.WeaponConfigId
            });
        }

        private bool TryFindNearestTarget(
            Entity vehicleEntity,
            float2 position,
            BattleTeamType team,
            float searchRange,
            out Entity target,
            out float2 targetPosition)
        {
            target = default;
            targetPosition = default;
            var bestDistance = float.MaxValue;
            spatialHashSystem.GetOpponentsInRange(position, searchRange, team, candidateBuffer);
            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (candidate.Equals(vehicleEntity) ||
                    !world.Has(candidate) ||
                    !positions.Has(candidate) ||
                    !healths.Has(candidate) ||
                    healths.Get(candidate).Current <= 0)
                {
                    continue;
                }

                var candidatePosition = positions.Get(candidate).Value;
                var distance = math.distancesq(position, candidatePosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                target = candidate;
                targetPosition = candidatePosition;
            }

            return world.Has(target);
        }

        private void ApplyVehicleMovement(
            Entity vehicleEntity,
            float2 position,
            float2 targetPosition,
            WeaponStatsComponent weapon)
        {
            var toTarget = targetPosition - position;
            var preferredRange = weapon.Range * PreferredRangeMultiplier;
            velocities.Get(vehicleEntity).Value = math.lengthsq(toTarget) > preferredRange * preferredRange
                ? ResolveVehicleMoveDirection(position, targetPosition)
                : float2.zero;
        }

        private float2 ResolveVehicleMoveDirection(float2 position, float2 targetPosition)
        {
            if (tileMap == null)
            {
                return math.normalizesafe(targetPosition - position);
            }

            var targetCell = ResolveCell(targetPosition);
            if (!TryResolveEnterableTarget(targetCell, out var resolvedTarget))
            {
                return math.normalizesafe(targetPosition - position);
            }

            var flowField = flowFieldBuilder.Build(tileMap, resolvedTarget.x, resolvedTarget.y, MoveLayer.Vehicle);
            if (flowField.TryGetDirection(position, out var direction) &&
                math.lengthsq(direction) > 0.0001f)
            {
                return direction;
            }

            var toResolvedTarget = new float2(resolvedTarget.x + 0.5f, resolvedTarget.y + 0.5f) - position;
            return math.normalizesafe(toResolvedTarget);
        }

        private int2 ResolveCell(float2 position)
        {
            return new int2(
                math.clamp((int)math.floor(position.x), 0, tileMap.Width - 1),
                math.clamp((int)math.floor(position.y), 0, tileMap.Height - 1));
        }

        private bool TryResolveEnterableTarget(int2 targetCell, out int2 resolvedTarget)
        {
            if (IsVehicleEnterable(targetCell.x, targetCell.y))
            {
                resolvedTarget = targetCell;
                return true;
            }

            var maxRadius = math.max(tileMap.Width, tileMap.Height);
            for (var radius = 1; radius <= maxRadius; radius++)
            {
                for (var y = targetCell.y - radius; y <= targetCell.y + radius; y++)
                {
                    for (var x = targetCell.x - radius; x <= targetCell.x + radius; x++)
                    {
                        if (math.abs(x - targetCell.x) != radius &&
                            math.abs(y - targetCell.y) != radius)
                        {
                            continue;
                        }

                        if (IsVehicleEnterable(x, y))
                        {
                            resolvedTarget = new int2(x, y);
                            return true;
                        }
                    }
                }
            }

            resolvedTarget = default;
            return false;
        }

        private bool IsVehicleEnterable(int x, int y)
        {
            if (tileMap == null || !tileMap.IsInside(x, y))
            {
                return false;
            }

            var tile = tileMap.GetTile(x, y);
            return tile.Walkable &&
                   (tile.AllowedMoveLayers & MoveLayer.Vehicle) != 0;
        }

        private static BattleTileMap TryCreateTileMap(BattleModel model)
        {
            if (model == null ||
                model.Width <= 0 ||
                model.Height <= 0 ||
                model.Tiles == null ||
                model.Tiles.Length != model.Width * model.Height)
            {
                return null;
            }

            return new BattleTileMap(model.Width, model.Height, model.Tiles);
        }
    }
}
