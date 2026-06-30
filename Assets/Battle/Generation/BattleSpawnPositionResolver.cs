using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    internal static class BattleSpawnPositionResolver
    {
        public static float2 ResolveCenter(BattleSpawnPoint spawnPoint, BattleMapGenerationConfig mapConfig)
        {
            if (mapConfig == null)
            {
                throw new ArgumentNullException(nameof(mapConfig));
            }

            var offset = mapConfig.UnitSpawnOffset;
            return new float2(spawnPoint.X + offset.x, spawnPoint.Y + offset.y);
        }

        public static float2 ResolveUnit(
            BattleSpawnPoint spawnPoint,
            BattleMapGenerationConfig mapConfig,
            int factionId,
            BattleTeamType team,
            int unitConfigId,
            int spawnIndex)
        {
            var center = ResolveCenter(spawnPoint, mapConfig);
            return center + ResolveJitter(
                factionId,
                team,
                unitConfigId,
                spawnIndex,
                mapConfig.UnitSpawnJitterRadius);
        }

        private static float2 ResolveJitter(
            int factionId,
            BattleTeamType team,
            int unitConfigId,
            int spawnIndex,
            float radius)
        {
            if (radius <= 0f)
            {
                return float2.zero;
            }

            var hash = Hash(factionId, (int)team, unitConfigId, spawnIndex);
            var angle = HashToUnit(hash) * math.PI * 2f;
            var distance = HashToUnit(hash * 1664525u + 1013904223u) * radius;
            return new float2(math.cos(angle), math.sin(angle)) * distance;
        }

        private static uint Hash(int a, int b, int c, int d)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = (hash ^ (uint)a) * 16777619u;
                hash = (hash ^ (uint)b) * 16777619u;
                hash = (hash ^ (uint)c) * 16777619u;
                hash = (hash ^ (uint)d) * 16777619u;
                return hash;
            }
        }

        private static float HashToUnit(uint hash)
        {
            return (hash & 0x00FFFFFFu) / (float)0x01000000u;
        }
    }
}
