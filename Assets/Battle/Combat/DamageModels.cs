using System;
using MercLord.Game.Configs;
using UnityEngine;

namespace MercLord.Battle.Combat
{
    [Serializable]
    public struct DamageRequest
    {
        public int SourceEntityId;
        public int TargetEntityId;
        public DamageType DamageType;
        public int RawDamage;
    }

    [Serializable]
    public struct ArmorValues
    {
        public int BallisticProtection;
        public int EnergyProtection;
        public int ExplosionProtection;
    }

    public static class DamageResolver
    {
        public static int ResolveFinalDamage(DamageRequest request, ArmorValues armor)
        {
            var protection = GetProtection(request.DamageType, armor);
            return Mathf.Max(0, request.RawDamage - protection);
        }

        private static int GetProtection(DamageType damageType, ArmorValues armor)
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
