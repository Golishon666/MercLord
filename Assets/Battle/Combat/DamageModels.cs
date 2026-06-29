using System;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Battle.Combat
{
    public struct DamageRequest
    {
        public Entity Source;
        public Entity Target;
        public float2 HitPosition;
        public DamageType DamageType;
        public int Amount;
    }

    [Serializable]
    public struct ArmorValues
    {
        public int BallisticProtection;
        public int EnergyProtection;
        public int ExplosionProtection;
    }

    [Serializable]
    public struct HealthValues
    {
        public int Current;
        public int Max;

        public bool IsDead => Current <= 0;
    }

    public readonly struct DamageResolution
    {
        public DamageResolution(DamageRequest request, int protection, int finalDamage, HealthValues healthAfterDamage)
        {
            Request = request;
            Protection = protection;
            FinalDamage = finalDamage;
            HealthAfterDamage = healthAfterDamage;
        }

        public DamageRequest Request { get; }
        public int Protection { get; }
        public int FinalDamage { get; }
        public HealthValues HealthAfterDamage { get; }
        public bool Killed => HealthAfterDamage.IsDead;
    }

    public interface IDamageSystem
    {
        DamageResolution ApplyDamage(
            DamageRequest request,
            ArmorValues armor,
            HealthValues health,
            DamageFormula formula);
    }

    public sealed class DamageSystem : IDamageSystem
    {
        public DamageResolution ApplyDamage(
            DamageRequest request,
            ArmorValues armor,
            HealthValues health,
            DamageFormula formula)
        {
            var protection = ArmorResolver.GetProtection(request.DamageType, armor);
            var finalDamage = DamageResolver.ResolveFinalDamage(request.Amount, protection, formula);
            health.Current = Mathf.Max(0, health.Current - finalDamage);
            return new DamageResolution(request, protection, finalDamage, health);
        }
    }

    public static class DamageResolver
    {
        public static int ResolveFinalDamage(int incomingDamage, int protection, DamageFormula formula)
        {
            return Mathf.Max(formula.MinimumDamage, incomingDamage - protection);
        }
    }

    public static class ArmorResolver
    {
        public static int GetProtection(DamageType damageType, ArmorValues armor)
        {
            switch (damageType)
            {
                case DamageType.Ballistic:
                    return armor.BallisticProtection;
                case DamageType.Energy:
                    return armor.EnergyProtection;
                case DamageType.Explosion:
                    return armor.ExplosionProtection;
                default:
                    throw new ArgumentOutOfRangeException(nameof(damageType), damageType, null);
            }
        }
    }
}
