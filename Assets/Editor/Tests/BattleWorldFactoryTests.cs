using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleWorldFactoryTests
    {
        [Test]
        public void WorldFactoryCreatesFiveHundredVsFiveHundredDummyUnits()
        {
            using var configSet = new TestConfigSet();
            var mapGenerator = new ConfigDrivenBattleMapGenerator(configSet.Database);
            var model = mapGenerator.Generate(
                new BattleGenerationRequest
                {
                    Seed = 12345,
                    SourceCellId = 6,
                    Biome = BiomeType.Plains,
                    HasRoad = true,
                    Height = 0.35f
                },
                CreateArmy(configSet.AttackerFaction.Id, configSet.RangedUnit.Id, 500),
                CreateArmy(configSet.DefenderFaction.Id, configSet.RangedUnit.Id, 500));
            var worldFactory = new ConfigDrivenBattleWorldFactory(configSet.Database, new BattleEntityFactory());
            World world = null;

            try
            {
                world = worldFactory.CreateWorld(model);

                Assert.AreEqual(1000, CountBots(world));
                Assert.AreEqual(500, CountTeam(world, BattleTeamType.Attacker));
                Assert.AreEqual(500, CountTeam(world, BattleTeamType.Defender));
                Assert.AreEqual(0, CountViews(world), "World factory must create data entities only; views are spawned by BattleViewSpawner.");
                AssertSquadEntities(world, model);
                AssertSquadMembership(world);
                AssertSpawnPositionsUseConfiguredLayout(world, configSet.Database.BattleMapGeneration);
            }
            finally
            {
                worldFactory.DisposeWorld(world);
            }
        }

        [Test]
        public void WorldFactoryRejectsArmyLargerThanSpawnCapacity()
        {
            using var configSet = new TestConfigSet();
            var mapGenerator = new ConfigDrivenBattleMapGenerator(configSet.Database);
            var model = mapGenerator.Generate(
                new BattleGenerationRequest
                {
                    Biome = BiomeType.Plains
                },
                CreateArmy(configSet.AttackerFaction.Id, configSet.RangedUnit.Id, 2000),
                new BattleArmyData());
            var worldFactory = new ConfigDrivenBattleWorldFactory(configSet.Database, new BattleEntityFactory());

            Assert.Throws<InvalidOperationException>(() => worldFactory.CreateWorld(model));
        }

        [Test]
        public void WorldFactoryAllowsEmptySideForPlayerOnlyBattle()
        {
            using var configSet = new TestConfigSet();
            var mapGenerator = new ConfigDrivenBattleMapGenerator(configSet.Database);
            var model = mapGenerator.Generate(
                new BattleGenerationRequest
                {
                    Biome = BiomeType.Plains
                },
                new BattleArmyData(),
                CreateArmy(configSet.DefenderFaction.Id, configSet.RangedUnit.Id, 1));
            var worldFactory = new ConfigDrivenBattleWorldFactory(configSet.Database, new BattleEntityFactory());
            World world = null;

            try
            {
                world = worldFactory.CreateWorld(model);

                Assert.AreEqual(1, CountBots(world));
                Assert.AreEqual(0, CountTeam(world, BattleTeamType.Attacker));
                Assert.AreEqual(1, CountTeam(world, BattleTeamType.Defender));
            }
            finally
            {
                worldFactory.DisposeWorld(world);
            }
        }

        private static BattleArmyData CreateArmy(int factionId, int unitConfigId, int count)
        {
            return new BattleArmyData
            {
                FactionId = factionId,
                Squads = new[]
                {
                    new SquadData
                    {
                        UnitConfigId = unitConfigId,
                        Count = count
                    }
                }
            };
        }

        private static int CountBots(World world)
        {
            var filter = world.Filter
                .With<BotComponent>()
                .Build();
            var count = 0;
            foreach (var _ in filter)
            {
                count++;
            }

            filter.Dispose();
            return count;
        }

        private static int CountViews(World world)
        {
            var filter = world.Filter
                .With<ViewRefComponent>()
                .Build();
            var count = 0;
            foreach (var _ in filter)
            {
                count++;
            }

            filter.Dispose();
            return count;
        }

        private static void AssertSquadMembership(World world)
        {
            var filter = world.Filter
                .With<BotComponent>()
                .With<TeamComponent>()
                .With<SquadMemberComponent>()
                .With<FormationSlotComponent>()
                .Build();
            var squads = world.GetStash<SquadMemberComponent>();
            var formationSlots = world.GetStash<FormationSlotComponent>();
            var countsBySquad = new Dictionary<int, int>();
            var checkedCount = 0;
            var sawNonZeroFormationOffset = false;

            foreach (var entity in filter)
            {
                var squad = squads.Get(entity);
                var formationSlot = formationSlots.Get(entity);
                Assert.GreaterOrEqual(squad.SquadId, 0);
                Assert.AreEqual(500, squad.SquadSize);
                Assert.GreaterOrEqual(squad.SlotIndex, 0);
                Assert.Less(squad.SlotIndex, squad.SquadSize);
                if (math.lengthsq(formationSlot.LocalOffset) > 0.0001f)
                {
                    sawNonZeroFormationOffset = true;
                }

                countsBySquad.TryGetValue(squad.SquadId, out var count);
                countsBySquad[squad.SquadId] = count + 1;
                checkedCount++;
            }

            filter.Dispose();
            Assert.AreEqual(1000, checkedCount);
            Assert.AreEqual(2, countsBySquad.Count);
            foreach (var count in countsBySquad.Values)
            {
                Assert.AreEqual(500, count);
            }

            Assert.IsTrue(sawNonZeroFormationOffset);
        }

        private static void AssertSquadEntities(World world, BattleModel model)
        {
            var filter = world.Filter
                .With<SquadComponent>()
                .With<SquadAnchorComponent>()
                .With<SquadOrderComponent>()
                .With<SquadMoraleComponent>()
                .Build();
            var squads = world.GetStash<SquadComponent>();
            var anchors = world.GetStash<SquadAnchorComponent>();
            var orders = world.GetStash<SquadOrderComponent>();
            var morale = world.GetStash<SquadMoraleComponent>();
            var checkedCount = 0;

            foreach (var entity in filter)
            {
                var squad = squads.Get(entity);
                var anchor = anchors.Get(entity);
                var order = orders.Get(entity);
                var squadMorale = morale.Get(entity);
                Assert.GreaterOrEqual(squad.SquadId, 0);
                Assert.AreEqual(500, squad.MemberCount);
                Assert.Greater(math.lengthsq(anchor.ForwardDirection), 0.99f);
                Assert.AreEqual(SquadOrderType.AttackNearest, order.Value);
                Assert.AreEqual(model.Objectives[0].Center.x, order.TargetPosition.x, 0.001f);
                Assert.AreEqual(model.Objectives[0].Center.y, order.TargetPosition.y, 0.001f);
                Assert.AreEqual(100f, squadMorale.Current);
                Assert.AreEqual(100f, squadMorale.Max);
                Assert.AreEqual(35f, squadMorale.RoutThreshold);
                Assert.IsFalse(squadMorale.IsRouted);
                checkedCount++;
            }

            filter.Dispose();
            Assert.AreEqual(2, checkedCount);
        }

        private static int CountTeam(World world, BattleTeamType team)
        {
            var filter = world.Filter
                .With<BotComponent>()
                .With<TeamComponent>()
                .Build();
            var teamStash = world.GetStash<TeamComponent>();
            var count = 0;
            foreach (var entity in filter)
            {
                if (teamStash.Get(entity).Value == team)
                {
                    count++;
                }
            }

            filter.Dispose();
            return count;
        }

        private static void AssertSpawnPositionsUseConfiguredLayout(
            World world,
            BattleMapGenerationConfig mapConfig)
        {
            var filter = world.Filter
                .With<BotComponent>()
                .With<PositionComponent>()
                .Build();
            var positionStash = world.GetStash<PositionComponent>();
            var checkedCount = 0;
            var sawJitteredPosition = false;

            foreach (var entity in filter)
            {
                var position = positionStash.Get(entity).Value;
                var localX = Fraction(position.x);
                var localY = Fraction(position.y);
                var offset = mapConfig.UnitSpawnOffset;
                var distanceFromOffset = math.distance(new float2(localX, localY), new float2(offset.x, offset.y));
                Assert.LessOrEqual(distanceFromOffset, mapConfig.UnitSpawnJitterRadius + 0.0001f);

                if (distanceFromOffset > 0.0001f)
                {
                    sawJitteredPosition = true;
                }

                checkedCount++;
            }

            filter.Dispose();
            Assert.AreEqual(1000, checkedCount);
            Assert.IsTrue(sawJitteredPosition, "A positive configured jitter radius should produce non-centered unit positions.");
        }

        private static float Fraction(float value)
        {
            return value - math.floor(value);
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

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                AttackerFaction = CreateConfig<FactionConfig>(1, "Test Attacker");
                DefenderFaction = CreateConfig<FactionConfig>(2, "Test Defender");
                var weapon = CreateConfig<WeaponConfig>(101, "Test Rifle");
                var armor = CreateConfig<ArmorConfig>(102, "Test Armor");
                var ai = CreateConfig<AIConfig>(103, "Test Passive AI");
                RangedUnit = CreateConfig<UnitConfig>(104, "Test Dummy Infantry");
                var tileSet = CreateConfig<TileSetConfig>(201, "Test Plains Tile Set");
                var biome = CreateConfig<BiomeConfig>(202, "Test Plains Biome");
                var mapGeneration = CreateConfig<BattleMapGenerationConfig>(301, "Test Battle Map");

                SetField(AttackerFaction, "startingCredits", 0);
                SetField(AttackerFaction, "startingStrength", 0);
                SetField(AttackerFaction, "capitalCellId", 0);
                SetField(DefenderFaction, "startingCredits", 0);
                SetField(DefenderFaction, "startingStrength", 0);
                SetField(DefenderFaction, "capitalCellId", 1);

                SetField(weapon, "damage", 1);
                SetField(weapon, "range", 6f);
                SetField(weapon, "cooldown", 1f);
                SetField(weapon, "projectileSpeed", 1f);

                SetField(armor, "ballisticProtection", 0);
                SetField(armor, "energyProtection", 0);
                SetField(armor, "explosionProtection", 0);

                SetField(ai, "type", AIType.Passive);
                SetField(ai, "thinkInterval", 1f);
                SetField(ai, "targetSearchRadius", 1f);
                SetField(ai, "preferredAttackDistance", 0f);
                SetField(ai, "retreatHealthPercent", 0f);

                SetField(RangedUnit, "factionId", AttackerFaction.Id);
                SetField(RangedUnit, "category", UnitCategory.RangedInfantry);
                SetField(RangedUnit, "maxHealth", 10);
                SetField(RangedUnit, "moveSpeed", 1f);
                SetField(RangedUnit, "rotationSpeed", 1f);
                SetField(RangedUnit, "weapon", weapon);
                SetField(RangedUnit, "armor", armor);
                SetField(RangedUnit, "ai", ai);
                SetField(RangedUnit, "viewPrefabAddress", "default_infantry");

                SetField(tileSet, "biomeType", BiomeType.Plains);
                SetField(biome, "biomeType", BiomeType.Plains);
                SetField(biome, "tileSetId", tileSet.Id);
                SetField(biome, "isPassableByDefault", true);

                SetField(mapGeneration, "width", 128);
                SetField(mapGeneration, "height", 96);
                SetField(mapGeneration, "defaultMoveCost", 2);
                SetField(mapGeneration, "roadMoveCost", 1);
                SetField(mapGeneration, "defaultCover", 0);
                SetField(mapGeneration, "settlementCover", 2);
                SetField(mapGeneration, "maxTileHeight", 4);
                SetField(mapGeneration, "roadColumn", 63);
                SetField(mapGeneration, "roadWidth", 3);
                SetField(mapGeneration, "attackerSpawnColumns", 12);
                SetField(mapGeneration, "defenderSpawnColumns", 12);
                SetField(mapGeneration, "unitSpawnOffset", new Vector2(0.5f, 0.5f));
                SetField(mapGeneration, "unitSpawnJitterRadius", 0.18f);

                SetField(Database, "factions", new[] { AttackerFaction, DefenderFaction });
                SetField(Database, "units", new[] { RangedUnit });
                SetField(Database, "weapons", new[] { weapon });
                SetField(Database, "armors", new[] { armor });
                SetField(Database, "aiConfigs", new[] { ai });
                SetField(Database, "biomes", new[] { biome });
                SetField(Database, "tileSets", new[] { tileSet });
                SetField(Database, "battleMapGeneration", mapGeneration);
            }

            public ConfigDatabase Database { get; }
            public FactionConfig AttackerFaction { get; }
            public FactionConfig DefenderFaction { get; }
            public UnitConfig RangedUnit { get; }

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

            private T Create<T>()
                where T : ScriptableObject
            {
                var asset = ScriptableObject.CreateInstance<T>();
                assets.Add(asset);
                return asset;
            }

            private T CreateConfig<T>(int id, string displayName)
                where T : IdentifiedConfig
            {
                var config = Create<T>();
                SetField(config, "id", id);
                SetField(config, "displayName", displayName);
                return config;
            }
        }
    }
}
