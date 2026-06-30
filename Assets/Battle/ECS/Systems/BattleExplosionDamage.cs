using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    internal static class BattleExplosionDamage
    {
        public static bool SpawnDamageRequests(
            World world,
            SpatialHashSystem spatialHashSystem,
            Stash<PositionComponent> positions,
            Stash<TeamComponent> teams,
            Stash<DamageRequestComponent> damageRequests,
            Entity source,
            float2 hitPosition,
            DamageType damageType,
            int amount,
            float radius,
            List<Entity> candidateBuffer)
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

            if (teams == null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            if (damageRequests == null)
            {
                throw new ArgumentNullException(nameof(damageRequests));
            }

            if (candidateBuffer == null)
            {
                throw new ArgumentNullException(nameof(candidateBuffer));
            }

            candidateBuffer.Clear();
            if (radius <= 0f || amount <= 0)
            {
                return false;
            }

            if (world.Has(source) && teams.Has(source))
            {
                var sourceTeam = teams.Get(source);
                spatialHashSystem.GetOpponentsInRange(hitPosition, radius, sourceTeam.Value, candidateBuffer);
            }
            else
            {
                spatialHashSystem.GetEntitiesInRange(hitPosition, radius, candidateBuffer);
            }

            var spawnedAny = false;
            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var target = candidateBuffer[candidateIndex];
                if (!world.Has(target) || target.Equals(source) || !positions.Has(target))
                {
                    continue;
                }

                var requestEntity = world.CreateEntity();
                damageRequests.Set(requestEntity, new DamageRequestComponent
                {
                    Source = source,
                    Target = target,
                    HitPosition = hitPosition,
                    DamageType = damageType,
                    Amount = amount
                });
                spawnedAny = true;
            }

            candidateBuffer.Clear();
            return spawnedAny;
        }
    }
}
