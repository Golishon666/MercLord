using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleMapGeneratorTests
    {
        [Test]
        public void ConfigDrivenGeneratorBuildsTileMetadataAndSpawnZones()
        {
            using var configSet = new TestConfigSet(passableByDefault: true);
            var generator = new ConfigDrivenBattleMapGenerator(configSet.Database);

            var model = generator.Generate(
                new BattleGenerationRequest
                {
                    Seed = 77,
                    SourceCellId = 12,
                    Biome = BiomeType.Plains,
                    HasRoad = true,
                    NearSettlement = true,
                    Height = 0.75f
                },
                new BattleArmyData(),
                new BattleArmyData());

            Assert.AreEqual(8, model.Width);
            Assert.AreEqual(4, model.Height);
            Assert.AreEqual(32, model.Tiles.Length);

            var tileMap = new BattleTileMap(model.Width, model.Height, model.Tiles);
            var baseTile = tileMap.GetTile(0, 0);
            Assert.IsTrue(baseTile.Walkable);
            Assert.AreEqual(BattleTileSurface.Ground, baseTile.Surface);
            Assert.AreEqual(2, baseTile.MoveCost);
            Assert.AreEqual(CoverType.Medium, baseTile.Cover);
            Assert.AreEqual(3, baseTile.Height);
            Assert.AreEqual(MoveLayer.Infantry | MoveLayer.Vehicle, baseTile.AllowedMoveLayers);
            Assert.IsFalse(baseTile.BlocksLineOfSight);
            Assert.IsFalse(baseTile.BlocksProjectiles);
            Assert.AreEqual(0, baseTile.RegionId);

            var roadTile = tileMap.GetTile(3, 1);
            Assert.IsTrue(roadTile.Walkable);
            Assert.AreEqual(BattleTileSurface.Road, roadTile.Surface);
            Assert.AreEqual(1, roadTile.MoveCost);
            Assert.AreEqual(MoveLayer.Infantry | MoveLayer.Vehicle, roadTile.AllowedMoveLayers);
            Assert.IsFalse(roadTile.BlocksLineOfSight);
            Assert.IsFalse(roadTile.BlocksProjectiles);

            Assert.AreEqual(2, model.SpawnZones.Length);
            Assert.AreEqual(BattleSpawnSide.Attacker, model.SpawnZones[0].Side);
            Assert.AreEqual(new RectInt(0, 0, 2, 4), model.SpawnZones[0].Area);
            Assert.AreEqual(Vector2Int.right, model.SpawnZones[0].ForwardDirection);
            Assert.IsTrue(model.SpawnZones[0].Contains(1, 3));

            Assert.AreEqual(BattleSpawnSide.Defender, model.SpawnZones[1].Side);
            Assert.AreEqual(new RectInt(6, 0, 2, 4), model.SpawnZones[1].Area);
            Assert.AreEqual(Vector2Int.left, model.SpawnZones[1].ForwardDirection);
            Assert.IsTrue(model.SpawnZones[1].Contains(6, 0));

            Assert.AreEqual(1, model.Objectives.Length);
            Assert.AreEqual(BattleObjectiveType.ControlPoint, model.Objectives[0].Type);
            Assert.AreEqual(new RectInt(2, 0, 3, 3), model.Objectives[0].Area);
            Assert.AreEqual(1, model.Objectives[0].Priority);
            Assert.IsTrue(model.Objectives[0].Contains(3, 1));
            Assert.IsFalse(model.SpawnZones[0].Contains(model.Objectives[0].Area.xMin, model.Objectives[0].Area.yMin));
            Assert.IsFalse(model.SpawnZones[1].Contains(model.Objectives[0].Area.xMax - 1, model.Objectives[0].Area.yMin));

            Assert.AreEqual(8, model.AttackerSpawnPoints.Length);
            Assert.AreEqual(8, model.DefenderSpawnPoints.Length);
            Assert.AreEqual(0, model.AttackerSpawnPoints[0].X);
            Assert.AreEqual(0, model.AttackerSpawnPoints[0].Y);
            Assert.AreEqual(6, model.DefenderSpawnPoints[0].X);
            Assert.AreEqual(0, model.DefenderSpawnPoints[0].Y);
        }

        [Test]
        public void ConfigDrivenGeneratorUsesBiomePassabilityAndKeepsRoadWalkable()
        {
            using var configSet = new TestConfigSet(passableByDefault: false);
            var generator = new ConfigDrivenBattleMapGenerator(configSet.Database);

            var model = generator.Generate(
                new BattleGenerationRequest
                {
                    Biome = BiomeType.Plains,
                    HasRoad = true
                },
                new BattleArmyData(),
                new BattleArmyData());

            var tileMap = new BattleTileMap(model.Width, model.Height, model.Tiles);
            var baseTile = tileMap.GetTile(0, 0);
            Assert.IsFalse(baseTile.Walkable);
            Assert.AreEqual(BattleTileSurface.Obstacle, baseTile.Surface);
            Assert.AreEqual(MoveLayer.None, baseTile.AllowedMoveLayers);

            var roadTile = tileMap.GetTile(3, 0);
            Assert.IsTrue(roadTile.Walkable);
            Assert.AreEqual(MoveLayer.Infantry | MoveLayer.Vehicle, roadTile.AllowedMoveLayers);
        }

        [Test]
        public void ForestGenerationAddsDeterministicCoverAndObstaclesWithoutBlockingRoadOrSpawns()
        {
            using var configSet = new TestConfigSet(
                passableByDefault: true,
                biomeType: BiomeType.Forest,
                width: 16,
                height: 8,
                roadColumn: 7,
                roadWidth: 2);
            SetField(configSet.MapGeneration, "forestCoverPatchCount", 8);
            SetField(configSet.MapGeneration, "forestCoverPatchRadius", 2);
            SetField(configSet.MapGeneration, "forestObstaclePatchCount", 4);
            SetField(configSet.MapGeneration, "forestObstaclePatchRadius", 1);
            var generator = new ConfigDrivenBattleMapGenerator(configSet.Database);
            var request = new BattleGenerationRequest
            {
                Seed = 1403,
                SourceCellId = 44,
                Biome = BiomeType.Forest,
                HasRoad = true
            };

            var first = generator.Generate(request, new BattleArmyData(), new BattleArmyData());
            var second = generator.Generate(request, new BattleArmyData(), new BattleArmyData());

            Assert.IsTrue(
                first.Tiles.Any(tile => tile.Cover == CoverType.Medium && tile.Surface != BattleTileSurface.Obstacle),
                "Forest generation should place medium cover patches.");
            Assert.IsTrue(
                first.Tiles.Any(tile => tile.Surface == BattleTileSurface.Obstacle && !tile.Walkable),
                "Forest generation should place obstacle patches.");

            var tileMap = new BattleTileMap(first.Width, first.Height, first.Tiles);
            for (var y = 0; y < first.Height; y++)
            {
                for (var x = 7; x < 9; x++)
                {
                    var roadTile = tileMap.GetTile(x, y);
                    Assert.IsTrue(roadTile.Walkable);
                    Assert.AreEqual(BattleTileSurface.Road, roadTile.Surface);
                }

                for (var x = 0; x < 2; x++)
                {
                    Assert.AreNotEqual(BattleTileSurface.Obstacle, tileMap.GetTile(x, y).Surface);
                }

                for (var x = 14; x < 16; x++)
                {
                    Assert.AreNotEqual(BattleTileSurface.Obstacle, tileMap.GetTile(x, y).Surface);
                }
            }

            CollectionAssert.AreEqual(
                first.Tiles.Select(TileSignature).ToArray(),
                second.Tiles.Select(TileSignature).ToArray());
        }

        [Test]
        public void PlainsGenerationAddsLightCoverWithoutObstacles()
        {
            using var configSet = new TestConfigSet(passableByDefault: true);
            SetField(configSet.MapGeneration, "defaultCover", 0);
            SetField(configSet.MapGeneration, "plainsCoverPatchCount", 4);
            SetField(configSet.MapGeneration, "plainsCoverPatchRadius", 1);
            var generator = new ConfigDrivenBattleMapGenerator(configSet.Database);

            var model = generator.Generate(
                new BattleGenerationRequest
                {
                    Seed = 811,
                    SourceCellId = 3,
                    Biome = BiomeType.Plains,
                    HasRoad = true
                },
                new BattleArmyData(),
                new BattleArmyData());

            Assert.IsTrue(model.Tiles.Any(tile => tile.Cover == CoverType.Light));
            Assert.IsFalse(model.Tiles.Any(tile => tile.Surface == BattleTileSurface.Obstacle));
        }

        private static string TileSignature(BattleTile tile)
        {
            return $"{tile.Walkable}:{tile.Surface}:{tile.MoveCost}:{tile.Cover}:{tile.Height}:{tile.AllowedMoveLayers}:{tile.BlocksLineOfSight}:{tile.BlocksProjectiles}:{tile.RegionId}";
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

            public TestConfigSet(
                bool passableByDefault,
                BiomeType biomeType = BiomeType.Plains,
                int width = 8,
                int height = 4,
                int roadColumn = 3,
                int roadWidth = 2)
            {
                Database = Create<ConfigDatabase>();
                var mapGeneration = CreateConfig<BattleMapGenerationConfig>(1, "Test Battle Map");
                var tileSet = CreateConfig<TileSetConfig>(2, "Test Tile Set");
                var biome = CreateConfig<BiomeConfig>(3, "Test Biome");

                SetField(mapGeneration, "width", width);
                SetField(mapGeneration, "height", height);
                SetField(mapGeneration, "defaultMoveCost", 2);
                SetField(mapGeneration, "roadMoveCost", 1);
                SetField(mapGeneration, "defaultCover", 1);
                SetField(mapGeneration, "settlementCover", 2);
                SetField(mapGeneration, "maxTileHeight", 4);
                SetField(mapGeneration, "roadColumn", roadColumn);
                SetField(mapGeneration, "roadWidth", roadWidth);
                SetField(mapGeneration, "attackerSpawnColumns", 2);
                SetField(mapGeneration, "defenderSpawnColumns", 2);

                SetField(tileSet, "biomeType", biomeType);
                SetField(biome, "biomeType", biomeType);
                SetField(biome, "tileSetId", tileSet.Id);
                SetField(biome, "isPassableByDefault", passableByDefault);

                SetField(Database, "battleMapGeneration", mapGeneration);
                SetField(Database, "biomes", new[] { biome });
                SetField(Database, "tileSets", new[] { tileSet });
                MapGeneration = mapGeneration;
            }

            public ConfigDatabase Database { get; }
            public BattleMapGenerationConfig MapGeneration { get; private set; }

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
