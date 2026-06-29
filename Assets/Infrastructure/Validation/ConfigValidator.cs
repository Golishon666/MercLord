using System.Collections.Generic;
using MercLord.Game.Configs;
using UnityEngine;

namespace MercLord.Infrastructure.Validation
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public readonly struct ValidationIssue
    {
        public ValidationIssue(ValidationSeverity severity, Object context, string message)
        {
            Severity = severity;
            Context = context;
            Message = message;
        }

        public ValidationSeverity Severity { get; }
        public Object Context { get; }
        public string Message { get; }
    }

    public sealed class ConfigValidator
    {
        public List<ValidationIssue> Validate(ConfigDatabase database)
        {
            var issues = new List<ValidationIssue>();
            if (database == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, null, "ConfigDatabase is missing."));
                return issues;
            }

            ValidateUnits(database, issues);
            ValidateWeapons(database, issues);
            ValidateArmors(database, issues);
            return issues;
        }

        private static void ValidateUnits(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var unit in database.Units)
            {
                if (unit == null)
                {
                    continue;
                }

                if (unit.Weapon == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has no weapon."));
                }

                if (unit.Armor == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has no armor."));
                }

                if (unit.AI == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has no AI config."));
                }

                if (string.IsNullOrWhiteSpace(unit.ViewPrefabAddress))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has no view prefab address."));
                }

                if (unit.MaxHealth <= 0 || unit.MoveSpeed <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has invalid health or move speed."));
                }
            }
        }

        private static void ValidateWeapons(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var weapon in database.Weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (weapon.Damage <= 0 || weapon.Range <= 0f || weapon.Cooldown <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, weapon, $"{weapon.DisplayName} has invalid combat values."));
                }

                if (weapon.IsProjectile && weapon.ProjectileSpeed <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, weapon, $"{weapon.DisplayName} projectile speed must be positive."));
                }
            }
        }

        private static void ValidateArmors(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var armor in database.Armors)
            {
                if (armor == null)
                {
                    continue;
                }

                if (armor.BallisticProtection < 0 || armor.EnergyProtection < 0 || armor.ExplosionProtection < 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, armor, $"{armor.DisplayName} has negative protection."));
                }
            }
        }
    }
}
