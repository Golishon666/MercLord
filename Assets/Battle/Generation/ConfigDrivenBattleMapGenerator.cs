using System;
using System.Collections.Generic;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using UnityEngine;

namespace MercLord.Battle.Generation
{
    public sealed class ConfigDrivenBattleMapGenerator : IBattleMapGenerator
    {
        private readonly ConfigDatabase configDatabase;

        public ConfigDrivenBattleMapGenerator(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public BattleModel Generate(
            BattleGenerationRequest request,
            BattleArmyData attacker,
            BattleArmyData defender)
        {
            var config = configDatabase.BattleMapGeneration
                ?? throw new InvalidOperationException("BattleMapGenerationConfig is required.");

            ValidateConfig(config);
            ValidateTileSet(request.Biome);

            var tiles = GenerateTiles(request, config);
            return new BattleModel
            {
                Seed = request.Seed,
                SourceCellId = request.SourceCellId,
                Width = config.Width,
                Height = config.Height,
                Tiles = tiles,
                AttackerSpawnPoints = BuildSpawnPoints(
                    config.Width,
                    config.Height,
                    startColumn: 0,
                    columnCount: config.AttackerSpawnColumns),
                DefenderSpawnPoints = BuildSpawnPoints(
                    config.Width,
                    config.Height,
                    startColumn: config.Width - config.DefenderSpawnColumns,
                    columnCount: config.DefenderSpawnColumns),
                Attacker = attacker ?? new BattleArmyData(),
                Defender = defender ?? new BattleArmyData()
            };
        }

        private BattleTile[] GenerateTiles(BattleGenerationRequest request, BattleMapGenerationConfig config)
        {
            var tiles = new BattleTile[config.Width * config.Height];
            var baseTile = CreateBaseTile(request, config);

            for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
            {
                tiles[tileIndex] = baseTile;
            }

            if (!request.HasRoad)
            {
                return tiles;
            }

            ApplyRoad(tiles, config);
            return tiles;
        }

        private static BattleTile CreateBaseTile(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config)
        {
            return new BattleTile
            {
                Walkable = true,
                MoveCost = ToByte(config.DefaultMoveCost, nameof(config.DefaultMoveCost)),
                Cover = ToByte(
                    request.NearSettlement
                        ? Math.Max(config.DefaultCover, config.SettlementCover)
                        : config.DefaultCover,
                    nameof(config.DefaultCover)),
                Height = ToByte(
                    Mathf.RoundToInt(Mathf.Clamp01(request.Height) * config.MaxTileHeight),
                    nameof(config.MaxTileHeight))
            };
        }

        private static void ApplyRoad(BattleTile[] tiles, BattleMapGenerationConfig config)
        {
            var roadStart = config.RoadColumn;
            var roadEndExclusive = config.RoadColumn + config.RoadWidth;
            var roadMoveCost = ToByte(config.RoadMoveCost, nameof(config.RoadMoveCost));

            for (var y = 0; y < config.Height; y++)
            {
                for (var x = roadStart; x < roadEndExclusive; x++)
                {
                    var index = y * config.Width + x;
                    var tile = tiles[index];
                    tile.MoveCost = roadMoveCost;
                    tiles[index] = tile;
                }
            }
        }

        private static BattleSpawnPoint[] BuildSpawnPoints(
            int width,
            int height,
            int startColumn,
            int columnCount)
        {
            var spawnPoints = new List<BattleSpawnPoint>(columnCount * height);
            var endColumnExclusive = startColumn + columnCount;
            for (var y = 0; y < height; y++)
            {
                for (var x = startColumn; x < endColumnExclusive; x++)
                {
                    spawnPoints.Add(new BattleSpawnPoint { X = x, Y = y });
                }
            }

            return spawnPoints.ToArray();
        }

        private void ValidateTileSet(BiomeType biome)
        {
            var biomeConfig = FindBiomeConfig(biome);
            if (!configDatabase.TryGetTileSet(biomeConfig.TileSetId, out _))
            {
                throw new InvalidOperationException(
                    $"{biomeConfig.DisplayName} references missing TileSetConfig id {biomeConfig.TileSetId}.");
            }
        }

        private BiomeConfig FindBiomeConfig(BiomeType biome)
        {
            for (var biomeIndex = 0; biomeIndex < configDatabase.Biomes.Count; biomeIndex++)
            {
                var biomeConfig = configDatabase.Biomes[biomeIndex];
                if (biomeConfig != null && biomeConfig.BiomeType == biome)
                {
                    return biomeConfig;
                }
            }

            throw new InvalidOperationException($"Biome config is not registered for {biome}.");
        }

        private static void ValidateConfig(BattleMapGenerationConfig config)
        {
            if (config.Width <= 0 || config.Height <= 0)
            {
                throw new InvalidOperationException("Battle map dimensions must be positive.");
            }

            ValidateByteConfig(config.DefaultMoveCost, nameof(config.DefaultMoveCost));
            ValidateByteConfig(config.RoadMoveCost, nameof(config.RoadMoveCost));
            ValidateByteConfig(config.DefaultCover, nameof(config.DefaultCover));
            ValidateByteConfig(config.SettlementCover, nameof(config.SettlementCover));
            ValidateByteConfig(config.MaxTileHeight, nameof(config.MaxTileHeight));

            if (config.DefaultMoveCost <= 0 || config.RoadMoveCost <= 0)
            {
                throw new InvalidOperationException("Battle tile move costs must be positive.");
            }

            if (config.RoadWidth <= 0)
            {
                throw new InvalidOperationException("Battle map road width must be positive.");
            }

            if (config.RoadColumn < 0 || config.RoadColumn + config.RoadWidth > config.Width)
            {
                throw new InvalidOperationException("Battle map road columns must fit inside map width.");
            }

            if (config.AttackerSpawnColumns <= 0 ||
                config.DefenderSpawnColumns <= 0 ||
                config.AttackerSpawnColumns > config.Width ||
                config.DefenderSpawnColumns > config.Width)
            {
                throw new InvalidOperationException("Battle map spawn columns must be positive and fit inside map width.");
            }
        }

        private static void ValidateByteConfig(int value, string configName)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                throw new InvalidOperationException($"{configName} must fit into byte range.");
            }
        }

        private static byte ToByte(int value, string configName)
        {
            ValidateByteConfig(value, configName);
            return (byte)value;
        }
    }
}
