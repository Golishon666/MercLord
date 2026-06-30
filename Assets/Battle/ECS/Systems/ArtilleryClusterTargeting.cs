using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    internal static class ArtilleryClusterTargeting
    {
        public static bool IsClusterWeapon(WeaponStatsComponent weapon)
        {
            return weapon.UsesParabolicTrajectory && weapon.ExplosionRadius > 0f;
        }

        public static bool TryFindBestCluster(
            World world,
            SpatialHashSystem spatialHashSystem,
            Stash<PositionComponent> positions,
            Stash<HealthComponent> healths,
            float2 sourcePosition,
            BattleTeamType ownTeam,
            WeaponStatsComponent weapon,
            List<Entity> candidateBuffer,
            List<Entity> clusterBuffer,
            out float2 targetPosition)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (spatialHashSystem == null)
            {
                throw new ArgumentNullException(nameof(spatialHashSystem));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (healths == null)
            {
                throw new ArgumentNullException(nameof(healths));
            }

            if (candidateBuffer == null)
            {
                throw new ArgumentNullException(nameof(candidateBuffer));
            }

            if (clusterBuffer == null)
            {
                throw new ArgumentNullException(nameof(clusterBuffer));
            }

            targetPosition = default;
            if (!IsClusterWeapon(weapon) || weapon.Range <= 0f)
            {
                return false;
            }

            candidateBuffer.Clear();
            spatialHashSystem.GetOpponentsInRange(sourcePosition, weapon.Range, ownTeam, candidateBuffer);
            var bestCount = 0;
            var bestDistance = float.MaxValue;
            var bestPosition = float2.zero;
            var weaponRangeSquared = weapon.Range * weapon.Range;

            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (!IsValidTarget(world, positions, healths, candidate))
                {
                    continue;
                }

                var candidatePosition = positions.Get(candidate).Value;
                clusterBuffer.Clear();
                spatialHashSystem.GetOpponentsInRange(candidatePosition, weapon.ExplosionRadius, ownTeam, clusterBuffer);

                var count = 0;
                var sum = float2.zero;
                for (var clusterIndex = 0; clusterIndex < clusterBuffer.Count; clusterIndex++)
                {
                    var clustered = clusterBuffer[clusterIndex];
                    if (!IsValidTarget(world, positions, healths, clustered))
                    {
                        continue;
                    }

                    sum += positions.Get(clustered).Value;
                    count++;
                }

                if (count <= 0)
                {
                    continue;
                }

                var center = sum / count;
                if (math.distancesq(sourcePosition, center) > weaponRangeSquared)
                {
                    center = candidatePosition;
                }

                var distance = math.distancesq(sourcePosition, center);
                if (count < bestCount || (count == bestCount && distance >= bestDistance))
                {
                    continue;
                }

                bestCount = count;
                bestDistance = distance;
                bestPosition = center;
            }

            candidateBuffer.Clear();
            clusterBuffer.Clear();
            if (bestCount <= 0)
            {
                return false;
            }

            targetPosition = bestPosition;
            return true;
        }

        public static Entity CreateTargetMarker(
            World world,
            Stash<PositionComponent> positions,
            float2 targetPosition)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            var marker = world.CreateEntity();
            positions.Set(marker, new PositionComponent { Value = targetPosition });
            world.GetStash<ArtilleryTargetMarkerComponent>().Set(marker, new ArtilleryTargetMarkerComponent());
            return marker;
        }

        private static bool IsValidTarget(
            World world,
            Stash<PositionComponent> positions,
            Stash<HealthComponent> healths,
            Entity target)
        {
            return world.Has(target) &&
                   positions.Has(target) &&
                   (!healths.Has(target) || healths.Get(target).Current > 0);
        }
    }
}
