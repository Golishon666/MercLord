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
            ValidateAIConfigs(database, issues);
            ValidateArmors(database, issues);
            ValidateVehicles(database, issues);
            ValidateTradeGoods(database, issues);
            ValidateItems(database, issues);
            ValidateLootTables(database, issues);
            ValidateGeneration(database, issues);
            ValidateCombatBalance(database, issues);
            ValidateBattleSimulation(database, issues);
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
                else if (!HasWeaponItem(database, culture.StartingWeapon))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} starting weapon has no matching weapon ItemConfig."));
                }

                if (culture.StartingArmor == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} has no starting armor."));
                }
                else if (!HasArmorItem(database, culture.StartingArmor))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, culture, $"{culture.DisplayName} starting armor has no matching armor ItemConfig."));
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

                if (weapon.UsesParabolicTrajectory && weapon.ParabolicArcHeight <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, weapon, $"{weapon.DisplayName} parabolic arc height must be positive."));
                }
            }
        }

        private static void ValidateAIConfigs(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var ai in database.AIConfigs)
            {
                if (ai == null)
                {
                    continue;
                }

                if (ai.ThinkInterval <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, ai, $"{ai.DisplayName} think interval must be positive."));
                }

                if (ai.TargetSearchRadius <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, ai, $"{ai.DisplayName} target search radius must be positive."));
                }

                if (ai.PreferredAttackDistance < 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, ai, $"{ai.DisplayName} preferred attack distance cannot be negative."));
                }

                if (ai.RetreatHealthPercent < 0f || ai.RetreatHealthPercent > 1f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, ai, $"{ai.DisplayName} retreat health percent must be between zero and one."));
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

        private static void ValidateVehicles(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var vehicle in database.Vehicles)
            {
                if (vehicle == null)
                {
                    continue;
                }

                if (vehicle.MaxHealth <= 0 || vehicle.MoveSpeed <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} has invalid health or move speed."));
                }

                if (vehicle.RotationSpeed <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} has invalid rotation speed."));
                }

                if (vehicle.EnterRadius <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} enter radius must be positive."));
                }

                if (vehicle.ExitDistance <= 0f)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} exit distance must be positive."));
                }

                if (vehicle.Armor == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} has no armor."));
                }
                else if (!database.TryGetArmor(vehicle.Armor.Id, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} references a missing ArmorConfig."));
                }

                if (vehicle.Weapon == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} has no weapon."));
                }
                else if (!database.TryGetWeapon(vehicle.Weapon.Id, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} references a missing WeaponConfig."));
                }

                if (string.IsNullOrWhiteSpace(vehicle.ViewPrefabAddress))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, vehicle, $"{vehicle.DisplayName} has no view prefab address."));
                }
            }
        }


        private static void ValidateItems(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var item in database.Items)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Price < 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} has negative price."));
                }

                if (item.Category == ItemCategory.Weapon)
                {
                    if (item.Weapon == null)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} weapon item has no WeaponConfig."));
                    }
                    else if (!database.TryGetWeapon(item.Weapon.Id, out _))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} references a missing WeaponConfig."));
                    }

                    continue;
                }

                if (item.Category == ItemCategory.Armor || item.Category == ItemCategory.Helmet)
                {
                    if (item.Armor == null)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} armor item has no ArmorConfig."));
                    }
                    else if (!database.TryGetArmor(item.Armor.Id, out _))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} references a missing ArmorConfig."));
                    }

                    continue;
                }

                if (item.Category == ItemCategory.TradeGood)
                {
                    if (item.TradeGood == null)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} trade good item has no TradeGoodConfig."));
                    }
                    else if (!database.TryGetTradeGood(item.TradeGood.Id, out _))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, item, $"{item.DisplayName} references a missing TradeGoodConfig."));
                    }
                }
            }
        }

        private static void ValidateTradeGoods(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var tradeGood in database.TradeGoods)
            {
                if (tradeGood == null)
                {
                    continue;
                }

                if (tradeGood.BasePrice <= 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, tradeGood, $"{tradeGood.DisplayName} base price must be positive."));
                }
            }
        }

        private static void ValidateLootTables(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            foreach (var lootTable in database.LootTables)
            {
                if (lootTable == null)
                {
                    continue;
                }

                var entries = lootTable.Entries ?? System.Array.Empty<LootEntry>();
                for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    if (entry.Item == null)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, lootTable, $"{lootTable.DisplayName} loot entry {entryIndex} has no ItemConfig."));
                    }
                    else if (!database.TryGetItem(entry.Item.Id, out _))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, entry.Item, $"{lootTable.DisplayName} loot entry {entryIndex} references a missing ItemConfig."));
                    }

                    if (entry.MinCount <= 0 || entry.MaxCount <= 0 || entry.MaxCount < entry.MinCount)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, lootTable, $"{lootTable.DisplayName} loot entry {entryIndex} has invalid count range."));
                    }

                    if (entry.Weight <= 0f)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, lootTable, $"{lootTable.DisplayName} loot entry {entryIndex} weight must be positive."));
                    }
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

        private static void ValidateBattleSimulation(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.BattleSimulation == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database, "BattleSimulationConfig is missing."));
                return;
            }

            if (database.BattleSimulation.SpatialHashCellSize <= 0f)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation spatial hash cell size must be positive."));
            }

            if (database.BattleSimulation.PlayerUnit == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation player unit must be assigned."));
            }
            else
            {
                var playerUnit = database.BattleSimulation.PlayerUnit;
                if (!database.TryGetUnit(playerUnit.Id, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation player unit must be registered in ConfigDatabase."));
                }

                if (playerUnit.Category != UnitCategory.Player)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, playerUnit, $"{playerUnit.DisplayName} must use UnitCategory.Player to be used as the player unit."));
                }
            }

            if (database.BattleSimulation.PlayerSpawnPointIndex < 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation player spawn point index cannot be negative."));
            }

            if (database.BattleSimulation.PlayerAimDotThreshold < -1f ||
                database.BattleSimulation.PlayerAimDotThreshold > 1f)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation player aim dot threshold must be between -1 and 1."));
            }

            ValidatePlayerSpawnCapacity(database, issues);
            ValidateVehicleSpawns(database, issues);
        }

        private static void ValidatePlayerSpawnCapacity(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.BattleMapGeneration == null || database.BattleSimulation == null)
            {
                return;
            }

            var map = database.BattleMapGeneration;
            if (map.Height <= 0)
            {
                return;
            }

            var spawnColumns = database.BattleSimulation.PlayerSpawnSide == BattleSpawnSide.Attacker
                ? map.AttackerSpawnColumns
                : map.DefenderSpawnColumns;
            if (spawnColumns <= 0)
            {
                return;
            }

            var capacity = map.Height * spawnColumns;
            if (database.BattleSimulation.PlayerSpawnPointIndex >= capacity)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, "Battle simulation player spawn point index must fit generated spawn capacity."));
            }
        }

        private static void ValidateVehicleSpawns(ConfigDatabase database, ICollection<ValidationIssue> issues)
        {
            if (database.BattleSimulation == null)
            {
                return;
            }

            var spawns = database.BattleSimulation.VehicleSpawns;
            for (var spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
            {
                var spawn = spawns[spawnIndex];
                if (spawn.Vehicle == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, $"Battle vehicle spawn {spawnIndex} has no VehicleConfig."));
                    continue;
                }

                if (!database.TryGetVehicle(spawn.Vehicle.Id, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, spawn.Vehicle, $"Battle vehicle spawn {spawnIndex} vehicle must be registered in ConfigDatabase."));
                }

                if (!database.TryGetFaction(spawn.FactionId, out _))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, $"Battle vehicle spawn {spawnIndex} references a missing FactionConfig."));
                }

                if (spawn.SpawnPointIndex < 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, $"Battle vehicle spawn {spawnIndex} point index cannot be negative."));
                    continue;
                }

                ValidateVehicleSpawnCapacity(database, spawn, spawnIndex, issues);
            }
        }

        private static void ValidateVehicleSpawnCapacity(
            ConfigDatabase database,
            BattleVehicleSpawnConfig spawn,
            int spawnIndex,
            ICollection<ValidationIssue> issues)
        {
            if (database.BattleMapGeneration == null)
            {
                return;
            }

            var map = database.BattleMapGeneration;
            if (map.Height <= 0)
            {
                return;
            }

            var spawnColumns = spawn.SpawnSide == BattleSpawnSide.Attacker
                ? map.AttackerSpawnColumns
                : map.DefenderSpawnColumns;
            if (spawnColumns <= 0)
            {
                return;
            }

            var capacity = map.Height * spawnColumns;
            if (spawn.SpawnPointIndex >= capacity)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, database.BattleSimulation, $"Battle vehicle spawn {spawnIndex} point index must fit generated spawn capacity."));
            }
        }

        private static bool HasWeaponItem(ConfigDatabase database, WeaponConfig weapon)
        {
            foreach (var item in database.Items)
            {
                if (item != null &&
                    item.Category == ItemCategory.Weapon &&
                    item.Weapon != null &&
                    item.Weapon.Id == weapon.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasArmorItem(ConfigDatabase database, ArmorConfig armor)
        {
            foreach (var item in database.Items)
            {
                if (item != null &&
                    item.Category == ItemCategory.Armor &&
                    item.Armor != null &&
                    item.Armor.Id == armor.Id)
                {
                    return true;
                }
            }

            return false;
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
