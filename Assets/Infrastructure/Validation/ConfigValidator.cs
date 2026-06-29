using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
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
        private const int MaxByteValue = byte.MaxValue;

        public List<ValidationIssue> Validate(ConfigDatabase database)
        {
            var issues = new List<ValidationIssue>();
            if (database == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, null, "ConfigDatabase is missing."));
                return issues;
            }

            ValidateCultures(database, issues);
            ValidateUnits(database, issues);
            ValidateWeapons(database, issues);
            ValidateArmors(database, issues);
            ValidateGeneration(database, issues);
            ValidateCombatBalance(database, issues);
            return issues;
        }

        private static void ValidateCultures(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var culture in database.Cultures)
            {
                if (culture == null)
                {
                    continue;
                }

                if (culture.StartingCredits < 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} has negative starting credits."));
                }

                if (database.GlobalGeneration != null &&
                    database.GlobalGeneration.TargetCellCount > 0 &&
                    (culture.StartingCellId < 0 || culture.StartingCellId >= database.GlobalGeneration.TargetCellCount))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} starting cell id must point to a generated world cell."));
                }

                if (culture.StartingWeapon == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} has no starting weapon."));
                }

                if (culture.StartingArmor == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} has no starting armor."));
                }
            }
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

                if (unit.RotationSpeed <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} has invalid rotation speed."));
                }

                if (!database.TryGetFaction(unit.FactionId, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, unit, $"{unit.DisplayName} references a missing FactionConfig."));
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

        private static void ValidateGeneration(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.GlobalGeneration == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "GlobalGenerationConfig is missing."));
            }
            else
            {
                if (database.GlobalGeneration.TargetCellCount <= 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.GlobalGeneration, "Global generation config has invalid cell count."));
                }

                if (database.GlobalGeneration.TargetCellCount > 0 &&
                    (database.GlobalGeneration.PlayerStartCellId < 0 ||
                     database.GlobalGeneration.PlayerStartCellId >= database.GlobalGeneration.TargetCellCount))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.GlobalGeneration, "Player start cell id must point to a generated world cell."));
                }

                if (database.GlobalGeneration.StartingDominantInfluence <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.GlobalGeneration, "Starting dominant influence must be positive."));
                }
            }

            ValidateFactions(database, issues);
            ValidateBiomes(database, issues);

            ValidateBattleMapGeneration(database, issues);
        }

        private static void ValidateBattleMapGeneration(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.BattleMapGeneration == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "BattleMapGenerationConfig is missing."));
                return;
            }

            var config = database.BattleMapGeneration;
            if (config.Width <= 0 || config.Height <= 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, config, "Battle map generation config has invalid dimensions."));
            }

            ValidateByteRange(config, config.DefaultMoveCost, nameof(config.DefaultMoveCost), issues);
            ValidateByteRange(config, config.RoadMoveCost, nameof(config.RoadMoveCost), issues);
            ValidateByteRange(config, config.DefaultCover, nameof(config.DefaultCover), issues);
            ValidateByteRange(config, config.SettlementCover, nameof(config.SettlementCover), issues);
            ValidateByteRange(config, config.MaxTileHeight, nameof(config.MaxTileHeight), issues);

            if (config.DefaultMoveCost <= 0 || config.RoadMoveCost <= 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, config, "Battle map move costs must be positive."));
            }

            if (config.RoadWidth <= 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, config, "Battle map road width must be positive."));
            }

            if (config.Width > 0 && (config.RoadColumn < 0 || config.RoadColumn + config.RoadWidth > config.Width))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, config, "Battle map road columns must fit inside map width."));
            }

            if (config.Width > 0 &&
                (config.AttackerSpawnColumns <= 0 ||
                 config.DefenderSpawnColumns <= 0 ||
                 config.AttackerSpawnColumns > config.Width ||
                 config.DefenderSpawnColumns > config.Width))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, config, "Battle map spawn columns must be positive and fit inside map width."));
            }
        }

        private static void ValidateFactions(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.Factions.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "At least one FactionConfig is required."));
                return;
            }

            if (database.Factions.Count > Influence4.Capacity)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, $"Influence4 supports up to {Influence4.Capacity} configured factions."));
            }

            var usedFactionIds = new HashSet<int>();
            for (var factionSlot = 0; factionSlot < database.Factions.Count; factionSlot++)
            {
                var faction = database.Factions[factionSlot];
                if (faction == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database, $"Faction config slot {factionSlot} is empty."));
                    continue;
                }

                if (!usedFactionIds.Add(faction.Id))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, faction, $"{faction.DisplayName} duplicates faction id {faction.Id}."));
                }

                if (faction.StartingCredits < 0 || faction.StartingStrength < 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, faction, $"{faction.DisplayName} has negative starting faction values."));
                }

                if (database.GlobalGeneration != null &&
                    database.GlobalGeneration.TargetCellCount > 0 &&
                    (faction.CapitalCellId < 0 || faction.CapitalCellId >= database.GlobalGeneration.TargetCellCount))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, faction, $"{faction.DisplayName} capital cell id must point to a generated world cell."));
                }
            }
        }

        private static void ValidateBiomes(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.Biomes.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "At least one BiomeConfig is required."));
                return;
            }

            for (var biomeSlot = 0; biomeSlot < database.Biomes.Count; biomeSlot++)
            {
                if (database.Biomes[biomeSlot] == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database, $"Biome config slot {biomeSlot} is empty."));
                    continue;
                }

                if (!database.TryGetTileSet(database.Biomes[biomeSlot].TileSetId, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.Biomes[biomeSlot], $"{database.Biomes[biomeSlot].DisplayName} references a missing TileSetConfig."));
                }
            }
        }

        private static void ValidateCombatBalance(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.CombatBalance == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "CombatBalanceConfig is missing."));
                return;
            }

            if (database.CombatBalance.DamageFormula.MinimumDamage < 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.CombatBalance, "Damage formula minimum damage cannot be negative."));
            }
        }

        private static void ValidateByteRange(
            Object context,
            int value,
            string configName,
            ICollection<ValidationIssue> issues)
        {
            if (value < byte.MinValue || value > MaxByteValue)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, context, $"{configName} must fit into byte range."));
            }
        }
    }
}
