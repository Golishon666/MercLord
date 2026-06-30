using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using MercLord.Infrastructure.Validation;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class ConfigValidatorTests
    {
        [Test]
        public void ValidMinimumConfigDatabaseHasNoValidationErrors()
        {
            using var configSet = CreateValidConfigSet();

            var errors = ValidateErrors(configSet.Database);

            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
        }

        [Test]
        public void CultureConfigRulesMatchValidationContract()
        {
            using var configSet = CreateValidConfigSet();
            SetField(configSet.Culture, "startingCellId", GlobalGenerationConfig.MinimumTargetCellCount + 1);
            SetField(configSet.Culture, "startingCredits", -1);
            SetField(configSet.Culture, "startingWeapon", null);
            SetField(configSet.Culture, "startingArmor", null);

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "starting cell id must point to a generated world cell");
            AssertContains(messages, "negative starting credits");
            AssertContains(messages, "has no starting weapon");
            AssertContains(messages, "has no starting armor");
        }

        [Test]
        public void UnitAndVehicleViewPrefabAddressesAreRequired()
        {
            using var configSet = CreateValidConfigSet();
            SetField(configSet.PlayerUnit, "viewPrefabAddress", string.Empty);
            SetField(configSet.Vehicle, "viewPrefabAddress", string.Empty);

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "Test Player has no view prefab address");
            AssertContains(messages, "Test Vehicle has no view prefab address");
        }

        [Test]
        public void ItemConfigsMustReferenceRegisteredTypedConfigs()
        {
            using var configSet = CreateValidConfigSet();
            var orphanWeapon = configSet.CreateConfig<WeaponConfig>(901, "Orphan Weapon");
            var orphanArmor = configSet.CreateConfig<ArmorConfig>(902, "Orphan Armor");
            var orphanTradeGood = configSet.CreateConfig<TradeGoodConfig>(903, "Orphan Trade Good");

            SetField(configSet.WeaponItem, "weapon", orphanWeapon);
            SetField(configSet.ArmorItem, "armor", orphanArmor);
            SetField(configSet.TradeGoodItem, "tradeGood", orphanTradeGood);

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "Weapon Item references a missing WeaponConfig");
            AssertContains(messages, "Armor Item references a missing ArmorConfig");
            AssertContains(messages, "Trade Good Item references a missing TradeGoodConfig");
        }

        [Test]
        public void BattleSimulationReferencesMustBeRegisteredAndFitSpawnCapacity()
        {
            using var configSet = CreateValidConfigSet();
            var orphanPlayer = configSet.CreateConfig<UnitConfig>(911, "Orphan Player");
            var orphanVehicle = configSet.CreateConfig<VehicleConfig>(912, "Orphan Vehicle");
            var orphanLootTable = configSet.CreateConfig<LootTableConfig>(913, "Orphan Loot Table");
            SetValidUnitFields(orphanPlayer, configSet.Faction.Id, UnitCategory.Player, configSet.Weapon, configSet.Armor, configSet.AI, "missing-player-prefab");
            SetValidVehicleFields(orphanVehicle, configSet.Armor, configSet.Weapon, "missing-vehicle-prefab");

            var invalidSpawn = WithField(default(BattleVehicleSpawnConfig), "vehicle", orphanVehicle);
            invalidSpawn = WithField(invalidSpawn, "spawnSide", BattleSpawnSide.Attacker);
            invalidSpawn = WithField(invalidSpawn, "spawnPointIndex", 999);
            invalidSpawn = WithField(invalidSpawn, "factionId", 999);

            SetField(configSet.BattleSimulation, "playerUnit", orphanPlayer);
            SetField(configSet.BattleSimulation, "playerSpawnPointIndex", 999);
            SetField(configSet.BattleSimulation, "victoryCreditsReward", -1);
            SetField(configSet.BattleSimulation, "victoryLootTable", orphanLootTable);
            SetField(configSet.BattleSimulation, "victoryLootRolls", -1);
            SetField(configSet.BattleSimulation, "victoryInfluenceReward", -1f);
            SetField(configSet.BattleSimulation, "vehicleSpawns", new[] { invalidSpawn });

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "player unit must be registered in ConfigDatabase");
            AssertContains(messages, "player spawn point index must fit generated spawn capacity");
            AssertContains(messages, "vehicle must be registered in ConfigDatabase");
            AssertContains(messages, "references a missing FactionConfig");
            AssertContains(messages, "point index must fit generated spawn capacity");
            AssertContains(messages, "victory credits reward cannot be negative");
            AssertContains(messages, "victory loot rolls cannot be negative");
            AssertContains(messages, "victory loot table must be registered in ConfigDatabase");
            AssertContains(messages, "victory influence reward cannot be negative");
        }

        [Test]
        public void BattleSimulationRewardTableRequiredWhenLootRollsEnabled()
        {
            using var configSet = CreateValidConfigSet();
            SetField(configSet.BattleSimulation, "victoryLootTable", null);
            SetField(configSet.BattleSimulation, "victoryLootRolls", 1);

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "victory loot table must be assigned when loot rolls are enabled");
        }

        [Test]
        public void BattleMapGenerationTileMetadataRulesMatchValidationContract()
        {
            using var configSet = CreateValidConfigSet();
            SetField(configSet.BattleMapGeneration, "defaultCover", (int)CoverType.Heavy + 1);
            SetField(configSet.BattleMapGeneration, "settlementCover", -1);
            SetField(configSet.BattleMapGeneration, "maxTileHeight", sbyte.MaxValue + 1);
            SetField(configSet.BattleMapGeneration, "plainsCoverPatchCount", -1);
            SetField(configSet.BattleMapGeneration, "forestCoverPatchCount", 1);
            SetField(configSet.BattleMapGeneration, "forestCoverPatchRadius", 0);
            SetField(configSet.BattleMapGeneration, "forestObstaclePatchRadius", -1);
            SetField(configSet.BattleMapGeneration, "unitSpawnOffset", new Vector2(1.25f, 0.5f));
            SetField(configSet.BattleMapGeneration, "unitSpawnJitterRadius", 0.75f);

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "DefaultCover must map to a supported CoverType");
            AssertContains(messages, "SettlementCover must map to a supported CoverType");
            AssertContains(messages, "MaxTileHeight must fit into signed byte range");
            AssertContains(messages, "PlainsCoverPatchCount cannot be negative");
            AssertContains(messages, "ForestCoverPatchCount radius must be positive");
            AssertContains(messages, "ForestObstaclePatchCount radius cannot be negative");
            AssertContains(messages, "Battle unit spawn offset must stay inside a tile");
            AssertContains(messages, "Battle unit spawn jitter radius must stay between zero and half a tile");
        }

        [Test]
        public void CombatBalanceHitChanceRulesMatchValidationContract()
        {
            using var configSet = CreateValidConfigSet();
            SetField(configSet.CombatBalance, "damageFormula", new DamageFormula { MinimumDamage = -1 });
            SetField(configSet.CombatBalance, "hitChanceFormula", new HitChanceFormula
            {
                BaseChance = 0.25f,
                MinimumChance = 0.5f,
                RangePenaltyAtMaxRange = -0.1f,
                LightCoverPenalty = 1.2f,
                MediumCoverPenalty = float.NaN,
                HeavyCoverPenalty = 0.2f,
                MovingTargetPenalty = 0.1f
            });

            var messages = ValidateErrorMessages(configSet.Database);

            AssertContains(messages, "Damage formula minimum damage cannot be negative");
            AssertContains(messages, "Hit chance range penalty must be between zero and one");
            AssertContains(messages, "Hit chance light cover penalty must be between zero and one");
            AssertContains(messages, "Hit chance medium cover penalty must be between zero and one");
            AssertContains(messages, "Hit chance minimum chance cannot exceed base chance");
        }

        private static ValidationIssue[] ValidateErrors(ConfigDatabase database)
        {
            return new ConfigValidator().Validate(database)
                .Where(issue => issue.Severity == ValidationSeverity.Error)
                .ToArray();
        }

        private static string[] ValidateErrorMessages(ConfigDatabase database)
        {
            return ValidateErrors(database)
                .Select(issue => issue.Message)
                .ToArray();
        }

        private static void AssertContains(IEnumerable<string> messages, string expectedSubstring)
        {
            Assert.IsTrue(
                messages.Any(message => message.Contains(expectedSubstring, StringComparison.Ordinal)),
                $"Expected validation message containing '{expectedSubstring}'. Actual:\n{string.Join("\n", messages)}");
        }

        private static TestConfigSet CreateValidConfigSet()
        {
            var configSet = new TestConfigSet();
            var database = configSet.Create<ConfigDatabase>();
            var faction = configSet.CreateConfig<FactionConfig>(1, "Test Faction");
            var weapon = configSet.CreateConfig<WeaponConfig>(101, "Test Weapon");
            var armor = configSet.CreateConfig<ArmorConfig>(102, "Test Armor");
            var ai = configSet.CreateConfig<AIConfig>(103, "Test AI");
            var playerUnit = configSet.CreateConfig<UnitConfig>(104, "Test Player");
            var vehicle = configSet.CreateConfig<VehicleConfig>(105, "Test Vehicle");
            var tradeGood = configSet.CreateConfig<TradeGoodConfig>(106, "Test Trade Good");
            var weaponItem = configSet.CreateConfig<ItemConfig>(201, "Weapon Item");
            var armorItem = configSet.CreateConfig<ItemConfig>(202, "Armor Item");
            var tradeGoodItem = configSet.CreateConfig<ItemConfig>(203, "Trade Good Item");
            var lootTable = configSet.CreateConfig<LootTableConfig>(301, "Test Loot Table");
            var tileSet = configSet.CreateConfig<TileSetConfig>(401, "Test Tile Set");
            var biome = configSet.CreateConfig<BiomeConfig>(402, "Test Biome");
            var combatBalance = configSet.CreateConfig<CombatBalanceConfig>(501, "Test Combat Balance");
            var globalGeneration = configSet.CreateConfig<GlobalGenerationConfig>(502, "Test Global Generation");
            var battleMapGeneration = configSet.CreateConfig<BattleMapGenerationConfig>(503, "Test Battle Map");
            var battleSimulation = configSet.CreateConfig<BattleSimulationConfig>(504, "Test Battle Simulation");
            var culture = configSet.CreateConfig<CultureConfig>(601, "Test Culture");

            SetField(faction, "startingCredits", 0);
            SetField(faction, "startingStrength", 0);
            SetField(faction, "capitalCellId", 0);

            SetField(weapon, "damage", 10);
            SetField(weapon, "range", 4f);
            SetField(weapon, "cooldown", 0.6f);
            SetField(weapon, "projectileSpeed", 12f);
            SetField(weapon, "isProjectile", true);
            SetField(weapon, "usesParabolicTrajectory", true);
            SetField(weapon, "parabolicArcHeight", 1.5f);

            SetField(armor, "ballisticProtection", 1);
            SetField(armor, "energyProtection", 1);
            SetField(armor, "explosionProtection", 1);

            SetField(ai, "thinkInterval", 0.2f);
            SetField(ai, "targetSearchRadius", 12f);
            SetField(ai, "preferredAttackDistance", 3f);
            SetField(ai, "retreatHealthPercent", 0.25f);

            SetValidUnitFields(playerUnit, faction.Id, UnitCategory.Player, weapon, armor, ai, "battle/player");
            SetValidVehicleFields(vehicle, armor, weapon, "battle/vehicle");

            SetField(tradeGood, "basePrice", 5);

            SetField(weaponItem, "category", ItemCategory.Weapon);
            SetField(weaponItem, "price", 12);
            SetField(weaponItem, "weapon", weapon);
            SetField(armorItem, "category", ItemCategory.Armor);
            SetField(armorItem, "price", 8);
            SetField(armorItem, "armor", armor);
            SetField(tradeGoodItem, "category", ItemCategory.TradeGood);
            SetField(tradeGoodItem, "price", 4);
            SetField(tradeGoodItem, "tradeGood", tradeGood);

            var lootEntry = WithField(default(LootEntry), "item", tradeGoodItem);
            lootEntry = WithField(lootEntry, "minCount", 1);
            lootEntry = WithField(lootEntry, "maxCount", 2);
            lootEntry = WithField(lootEntry, "weight", 1f);
            SetField(lootTable, "entries", new[] { lootEntry });

            SetField(tileSet, "biomeType", BiomeType.Plains);
            SetField(biome, "biomeType", BiomeType.Plains);
            SetField(biome, "tileSetId", tileSet.Id);
            SetField(biome, "isPassableByDefault", true);

            SetField(combatBalance, "damageFormula", new DamageFormula { MinimumDamage = 1 });
            SetField(combatBalance, "hitChanceFormula", HitChanceFormula.Default);

            SetField(globalGeneration, "targetCellCount", GlobalGenerationConfig.MinimumTargetCellCount);
            SetField(globalGeneration, "playerStartCellId", 0);
            SetField(globalGeneration, "startingDominantInfluence", GlobalGenerationConfig.DefaultStartingDominantInfluence);

            SetField(battleMapGeneration, "width", 8);
            SetField(battleMapGeneration, "height", 4);
            SetField(battleMapGeneration, "defaultMoveCost", 1);
            SetField(battleMapGeneration, "roadMoveCost", 1);
            SetField(battleMapGeneration, "defaultCover", 1);
            SetField(battleMapGeneration, "settlementCover", 2);
            SetField(battleMapGeneration, "maxTileHeight", 3);
            SetField(battleMapGeneration, "roadColumn", 3);
            SetField(battleMapGeneration, "roadWidth", 1);
            SetField(battleMapGeneration, "attackerSpawnColumns", 2);
            SetField(battleMapGeneration, "defenderSpawnColumns", 2);

            var vehicleSpawn = WithField(default(BattleVehicleSpawnConfig), "vehicle", vehicle);
            vehicleSpawn = WithField(vehicleSpawn, "spawnSide", BattleSpawnSide.Attacker);
            vehicleSpawn = WithField(vehicleSpawn, "spawnPointIndex", 0);
            vehicleSpawn = WithField(vehicleSpawn, "factionId", faction.Id);
            SetField(battleSimulation, "spatialHashCellSize", 2f);
            SetField(battleSimulation, "playerUnit", playerUnit);
            SetField(battleSimulation, "playerSpawnSide", BattleSpawnSide.Attacker);
            SetField(battleSimulation, "playerSpawnPointIndex", 0);
            SetField(battleSimulation, "playerAimDotThreshold", 0.25f);
            SetField(battleSimulation, "victoryCreditsReward", 25);
            SetField(battleSimulation, "victoryLootTable", lootTable);
            SetField(battleSimulation, "victoryLootRolls", 1);
            SetField(battleSimulation, "victoryInfluenceReward", 12f);
            SetField(battleSimulation, "vehicleSpawns", new[] { vehicleSpawn });

            SetField(culture, "startingCellId", 0);
            SetField(culture, "startingCredits", 0);
            SetField(culture, "startingWeapon", weapon);
            SetField(culture, "startingArmor", armor);

            SetField(database, "factions", new[] { faction });
            SetField(database, "cultures", new[] { culture });
            SetField(database, "units", new[] { playerUnit });
            SetField(database, "weapons", new[] { weapon });
            SetField(database, "armors", new[] { armor });
            SetField(database, "aiConfigs", new[] { ai });
            SetField(database, "vehicles", new[] { vehicle });
            SetField(database, "items", new[] { weaponItem, armorItem, tradeGoodItem });
            SetField(database, "tradeGoods", new[] { tradeGood });
            SetField(database, "lootTables", new[] { lootTable });
            SetField(database, "biomes", new[] { biome });
            SetField(database, "tileSets", new[] { tileSet });
            SetField(database, "combatBalance", combatBalance);
            SetField(database, "globalGeneration", globalGeneration);
            SetField(database, "battleMapGeneration", battleMapGeneration);
            SetField(database, "battleSimulation", battleSimulation);

            configSet.Database = database;
            configSet.Faction = faction;
            configSet.Weapon = weapon;
            configSet.Armor = armor;
            configSet.AI = ai;
            configSet.PlayerUnit = playerUnit;
            configSet.Vehicle = vehicle;
            configSet.WeaponItem = weaponItem;
            configSet.ArmorItem = armorItem;
            configSet.TradeGoodItem = tradeGoodItem;
            configSet.BattleSimulation = battleSimulation;
            configSet.BattleMapGeneration = battleMapGeneration;
            configSet.CombatBalance = combatBalance;
            configSet.Culture = culture;
            return configSet;
        }

        private static void SetValidUnitFields(
            UnitConfig unit,
            int factionId,
            UnitCategory category,
            WeaponConfig weapon,
            ArmorConfig armor,
            AIConfig ai,
            string viewPrefabAddress)
        {
            SetField(unit, "factionId", factionId);
            SetField(unit, "category", category);
            SetField(unit, "maxHealth", 100);
            SetField(unit, "moveSpeed", 3f);
            SetField(unit, "rotationSpeed", 360f);
            SetField(unit, "weapon", weapon);
            SetField(unit, "armor", armor);
            SetField(unit, "ai", ai);
            SetField(unit, "viewPrefabAddress", viewPrefabAddress);
        }

        private static void SetValidVehicleFields(
            VehicleConfig vehicle,
            ArmorConfig armor,
            WeaponConfig weapon,
            string viewPrefabAddress)
        {
            SetField(vehicle, "maxHealth", 300);
            SetField(vehicle, "moveSpeed", 4f);
            SetField(vehicle, "rotationSpeed", 180f);
            SetField(vehicle, "enterRadius", 1.5f);
            SetField(vehicle, "exitDistance", 2f);
            SetField(vehicle, "armor", armor);
            SetField(vehicle, "weapon", weapon);
            SetField(vehicle, "viewPrefabAddress", viewPrefabAddress);
        }

        private static T WithField<T>(T value, string fieldName, object fieldValue)
            where T : struct
        {
            object boxed = value;
            SetField(boxed, fieldName, fieldValue);
            return (T)boxed;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        }

        private sealed class TestConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public ConfigDatabase Database { get; set; }
            public FactionConfig Faction { get; set; }
            public WeaponConfig Weapon { get; set; }
            public ArmorConfig Armor { get; set; }
            public AIConfig AI { get; set; }
            public UnitConfig PlayerUnit { get; set; }
            public VehicleConfig Vehicle { get; set; }
            public ItemConfig WeaponItem { get; set; }
            public ItemConfig ArmorItem { get; set; }
            public ItemConfig TradeGoodItem { get; set; }
            public BattleSimulationConfig BattleSimulation { get; set; }
            public BattleMapGenerationConfig BattleMapGeneration { get; set; }
            public CombatBalanceConfig CombatBalance { get; set; }
            public CultureConfig Culture { get; set; }

            public T Create<T>()
                where T : ScriptableObject
            {
                var config = ScriptableObject.CreateInstance<T>();
                assets.Add(config);
                return config;
            }

            public T CreateConfig<T>(int id, string displayName)
                where T : IdentifiedConfig
            {
                var config = Create<T>();
                SetField(config, "id", id);
                SetField(config, "displayName", displayName);
                return config;
            }

            public void Dispose()
            {
                for (var assetIndex = assets.Count - 1; assetIndex >= 0; assetIndex--)
                {
                    if (assets[assetIndex] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(assets[assetIndex]);
                    }
                }
            }
        }
    }
}
