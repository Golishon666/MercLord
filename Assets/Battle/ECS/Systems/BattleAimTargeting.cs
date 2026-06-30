using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public static class BattleAimTargeting
    {
        public static bool TryFindAimedTarget(
            World world,
            SpatialHashSystem spatialHashSystem,
            ConfigDatabase configDatabase,
            Stash<PositionComponent> positions,
            Stash<TeamComponent> teams,
            Stash<HealthComponent> healths,
            Entity source,
            float2 aimDirection,
            float range,
            List<Entity> candidateBuffer,
            out Entity target)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (spatialHashSystem == null)
            {
                throw new ArgumentNullException(nameof(spatialHashSystem));
            }

            if (configDatabase == null)
            {
                throw new ArgumentNullException(nameof(configDatabase));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (teams == null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            if (healths == null)
            {
                throw new ArgumentNullException(nameof(healths));
            }

            if (candidateBuffer == null)
            {
                throw new ArgumentNullException(nameof(candidateBuffer));
            }

            target = default;
            if (world.IsDisposed ||
                range <= 0f ||
                math.lengthsq(aimDirection) <= float.Epsilon ||
                !positions.Has(source) ||
                !teams.Has(source))
            {
                return false;
            }

            var sourcePosition = positions.Get(source);
            var team = teams.Get(source);
            var normalizedAim = math.normalizesafe(aimDirection);
            spatialHashSystem.GetOpponentsInRange(
                sourcePosition.Value,
                range,
                team.Value,
                candidateBuffer);

            var threshold = configDatabase.BattleSimulation != null
                ? configDatabase.BattleSimulation.PlayerAimDotThreshold
                : 0f;
            var bestDot = threshold;
            var bestDistanceSquared = float.MaxValue;
            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (!world.Has(candidate) ||
                    !positions.Has(candidate) ||
                    !healths.Has(candidate))
                {
                    continue;
                }

                var candidatePosition = positions.Get(candidate);
                var toCandidate = candidatePosition.Value - sourcePosition.Value;
                var distanceSquared = math.lengthsq(toCandidate);
                if (distanceSquared <= float.Epsilon)
                {
                    continue;
                }

                var candidateDirection = math.normalizesafe(toCandidate);
                var dot = math.dot(normalizedAim, candidateDirection);
                if (dot < threshold)
                {
                    continue;
                }

                if (dot > bestDot || (math.abs(dot - bestDot) <= 0.0001f && distanceSquared < bestDistanceSquared))
                {
                    bestDot = dot;
                    bestDistanceSquared = distanceSquared;
                    target = candidate;
                }
            }

            return world.Has(target);
        }
    }
}
