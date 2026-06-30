using MercLord.Battle.Tiles;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    internal static class BattleHitChance
    {
        private const float MovingTargetVelocitySquared = 0.01f;
        private const float RollDivisor = 16777216f;

        public static float Calculate(
            HitChanceFormula formula,
            WeaponStatsComponent weapon,
            float2 sourcePosition,
            float2 targetPosition,
            float2 targetVelocity,
            CoverType targetCover)
        {
            var range = math.max(0.001f, weapon.Range);
            var distanceFraction = math.saturate(math.distance(sourcePosition, targetPosition) / range);
            var chance = formula.BaseChance - formula.RangePenaltyAtMaxRange * distanceFraction;
            chance -= GetCoverPenalty(formula, targetCover);
            if (math.lengthsq(targetVelocity) > MovingTargetVelocitySquared)
            {
                chance -= formula.MovingTargetPenalty;
            }

            return math.clamp(chance, math.clamp(formula.MinimumChance, 0f, 1f), 1f);
        }

        public static bool RollHits(
            int battleSeed,
            Entity source,
            Entity target,
            WeaponStatsComponent weapon,
            float2 sourcePosition,
            float2 targetPosition,
            float hitChance)
        {
            var roll = CreateRoll(
                battleSeed,
                source.GetHashCode(),
                target.GetHashCode(),
                weapon.WeaponConfigId,
                sourcePosition,
                targetPosition);
            return roll < hitChance;
        }

        private static float GetCoverPenalty(HitChanceFormula formula, CoverType cover)
        {
            switch (cover)
            {
                case CoverType.Light:
                    return formula.LightCoverPenalty;
                case CoverType.Medium:
                    return formula.MediumCoverPenalty;
                case CoverType.Heavy:
                    return formula.HeavyCoverPenalty;
                default:
                    return 0f;
            }
        }

        private static float CreateRoll(
            int battleSeed,
            int sourceHash,
            int targetHash,
            int weaponConfigId,
            float2 sourcePosition,
            float2 targetPosition)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Mix(hash, battleSeed);
                hash = Mix(hash, sourceHash);
                hash = Mix(hash, targetHash);
                hash = Mix(hash, weaponConfigId);
                hash = Mix(hash, Quantize(sourcePosition.x));
                hash = Mix(hash, Quantize(sourcePosition.y));
                hash = Mix(hash, Quantize(targetPosition.x));
                hash = Mix(hash, Quantize(targetPosition.y));
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / RollDivisor;
            }
        }

        private static uint Mix(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                return hash * 16777619u;
            }
        }

        private static int Quantize(float value)
        {
            return (int)math.round(value * 100f);
        }
    }
}
