using System;
using System.Collections.Generic;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using UnityEngine;

namespace MercLord.Battle.Generation
{
    public sealed class ConfigDrivenBattleMapGenerator : IBattleMapGenerator
    {
        private const float MaxSpawnJitterRadius = 0.49f;

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
            var biomeConfig = FindBiomeConfig(request.Biome);
            ValidateTileSet(biomeConfig);

            var tiles = GenerateTiles(request, config, biomeConfig);
            return new BattleModel
            {
                Seed = request.Seed,
                SourceCellId = request.SourceCellId,
                Width = config.Width,
                Height = config.Height,
                Tiles = tiles,
                SpawnZones = BuildSpawnZones(config),
                Objectives = BuildObjectives(config),
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

        private static BattleObjectiveZone[] BuildObjectives(BattleMapGenerationConfig config)
        {
            var neutralMinX = Mathf.Clamp(config.AttackerSpawnColumns, 0, config.Width - 1);
            var neutralMaxXExclusive = Mathf.Clamp(
                config.Width - config.DefenderSpawnColumns,
                neutralMinX + 1,
                config.Width);
            var availableWidth = Mathf.Max(1, neutralMaxXExclusive - neutralMinX);
            var objectiveWidth = Mathf.Clamp(Mathf.Max(3, config.Width / 10), 1, availableWidth);
            var objectiveHeight = Mathf.Clamp(Mathf.Max(3, config.Height / 8), 1, config.Height);
            var x = neutralMinX + (availableWidth - objectiveWidth) / 2;
            var y = (config.Height - objectiveHeight) / 2;

            return new[]
            {
                new BattleObjectiveZone
                {
                    Type = BattleObjectiveType.ControlPoint,
                    Area = new RectInt(x, y, objectiveWidth, objectiveHeight),
                    Priority = 1
                }
            };
        }

        private BattleTile[] GenerateTiles(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config,
            BiomeConfig biomeConfig)
        {
            var tiles = new BattleTile[config.Width * config.Height];
            var baseTile = CreateBaseTile(request, config, biomeConfig);

            for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
            {
                tiles[tileIndex] = baseTile;
            }

            if (!request.HasRoad)
            {
                ApplyBiomeFeatures(request, config, tiles);
                return tiles;
            }

            ApplyRoad(tiles, config);
            ApplyBiomeFeatures(request, config, tiles);
            return tiles;
        }

        private static BattleTile CreateBaseTile(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config,
            BiomeConfig biomeConfig)
        {
            var cover = ToCoverType(
                request.NearSettlement
                    ? Math.Max(config.DefaultCover, config.SettlementCover)
                    : config.DefaultCover,
                nameof(config.DefaultCover));
            var walkable = biomeConfig.IsPassableByDefault;
            return new BattleTile
            {
                Walkable = walkable,
                Surface = walkable ? BattleTileSurface.Ground : BattleTileSurface.Obstacle,
                MoveCost = ToByte(config.DefaultMoveCost, nameof(config.DefaultMoveCost)),
                Cover = cover,
                Height = ToSByte(
                    Mathf.RoundToInt(Mathf.Clamp01(request.Height) * config.MaxTileHeight),
                    nameof(config.MaxTileHeight)),
                AllowedMoveLayers = walkable ? MoveLayer.Infantry | MoveLayer.Vehicle : MoveLayer.None,
                BlocksLineOfSight = cover == CoverType.Heavy,
                BlocksProjectiles = cover == CoverType.Heavy,
                RegionId = 0
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
                    tile.Walkable = true;
                    tile.Surface = BattleTileSurface.Road;
                    tile.MoveCost = roadMoveCost;
                    tile.AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle;
                    tile.BlocksLineOfSight = false;
                    tile.BlocksProjectiles = false;
                    tiles[index] = tile;
                }
            }
        }

        private static void ApplyBiomeFeatures(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config,
            BattleTile[] tiles)
        {
            switch (request.Biome)
            {
                case BiomeType.Plains:
                    ApplyCoverPatches(
                        request,
                        config,
                        tiles,
                        config.PlainsCoverPatchCount,
                        config.PlainsCoverPatchRadius,
                        CoverType.Light,
                        salt: 101);
                    break;
                case BiomeType.Forest:
                    ApplyCoverPatches(
                        request,
                        config,
                        tiles,
                        config.ForestCoverPatchCount,
                        config.ForestCoverPatchRadius,
                        CoverType.Medium,
                        salt: 201);
                    ApplyObstaclePatches(
                        request,
                        config,
                        tiles,
                        config.ForestObstaclePatchCount,
                        config.ForestObstaclePatchRadius,
                        salt: 301);
                    break;
            }
        }

        private static void ApplyCoverPatches(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config,
            BattleTile[] tiles,
            int patchCount,
            int patchRadius,
            CoverType cover,
            int salt)
        {
            if (patchCount <= 0 || patchRadius <= 0)
            {
                return;
            }

            var random = new System.Random(CreateFeatureSeed(request, salt));
            for (var patchIndex = 0; patchIndex < patchCount; patchIndex++)
            {
                var center = PickFeatureCenter(random, config, request.HasRoad);
                ApplyPatch(config, center, patchRadius, (x, y) =>
                {
                    if (ShouldKeepTileClear(config, request.HasRoad, x, y))
                    {
                        return;
                    }

                    var index = y * config.Width + x;
                    var tile = tiles[index];
                    if (!tile.Walkable || tile.Surface == BattleTileSurface.Obstacle)
                    {
                        return;
                    }

                    tile.Cover = MaxCover(tile.Cover, cover);
                    tile.BlocksLineOfSight = tile.Cover == CoverType.Heavy;
                    tile.BlocksProjectiles = tile.Cover == CoverType.Heavy;
                    tiles[index] = tile;
                });
            }
        }

        private static void ApplyObstaclePatches(
            BattleGenerationRequest request,
            BattleMapGenerationConfig config,
            BattleTile[] tiles,
            int patchCount,
            int patchRadius,
            int salt)
        {
            if (patchCount <= 0 || patchRadius <= 0)
            {
                return;
            }

            var random = new System.Random(CreateFeatureSeed(request, salt));
            for (var patchIndex = 0; patchIndex < patchCount; patchIndex++)
            {
                var center = PickFeatureCenter(random, config, request.HasRoad);
                ApplyPatch(config, center, patchRadius, (x, y) =>
                {
                    if (ShouldKeepTileClear(config, request.HasRoad, x, y))
                    {
                        return;
                    }

                    var index = y * config.Width + x;
                    var tile = tiles[index];
                    tile.Walkable = false;
                    tile.Surface = BattleTileSurface.Obstacle;
                    tile.Cover = CoverType.Heavy;
                    tile.AllowedMoveLayers = MoveLayer.None;
                    tile.BlocksLineOfSight = true;
                    tile.BlocksProjectiles = true;
                    tiles[index] = tile;
                });
            }
        }

        private static Vector2Int PickFeatureCenter(
            System.Random random,
            BattleMapGenerationConfig config,
            bool hasRoad)
        {
            for (var attempt = 0; attempt < 16; attempt++)
            {
                var candidate = new Vector2Int(
                    random.Next(0, config.Width),
                    random.Next(0, config.Height));
                if (!ShouldKeepTileClear(config, hasRoad, candidate.x, candidate.y))
                {
                    return candidate;
                }
            }

            for (var y = 0; y < config.Height; y++)
            {
                for (var x = 0; x < config.Width; x++)
                {
                    if (!ShouldKeepTileClear(config, hasRoad, x, y))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(config.Width / 2, config.Height / 2);
        }

        private static void ApplyPatch(
            BattleMapGenerationConfig config,
            Vector2Int center,
            int radius,
            Action<int, int> applyTile)
        {
            var radiusSquared = radius * radius;
            var xMin = Mathf.Max(0, center.x - radius);
            var xMax = Mathf.Min(config.Width - 1, center.x + radius);
            var yMin = Mathf.Max(0, center.y - radius);
            var yMax = Mathf.Min(config.Height - 1, center.y + radius);

            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = x - center.x;
                    var dy = y - center.y;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    applyTile(x, y);
                }
            }
        }

        private static bool ShouldKeepTileClear(BattleMapGenerationConfig config, bool hasRoad, int x, int y)
        {
            return (hasRoad && IsRoadColumn(config, x)) ||
                   x < config.AttackerSpawnColumns ||
                   x >= config.Width - config.DefenderSpawnColumns;
        }

        private static bool IsRoadColumn(BattleMapGenerationConfig config, int x)
        {
            return x >= config.RoadColumn && x < config.RoadColumn + config.RoadWidth;
        }

        private static CoverType MaxCover(CoverType current, CoverType candidate)
        {
            return candidate > current ? candidate : current;
        }

        private static int CreateFeatureSeed(BattleGenerationRequest request, int salt)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + request.Seed;
                hash = hash * 31 + request.SourceCellId;
                hash = hash * 31 + (int)request.Biome;
                hash = hash * 31 + salt;
                return hash;
            }
        }

        private static BattleSpawnZone[] BuildSpawnZones(BattleMapGenerationConfig config)
        {
            return new[]
            {
                new BattleSpawnZone
                {
                    Side = BattleSpawnSide.Attacker,
                    Area = new RectInt(0, 0, config.AttackerSpawnColumns, config.Height),
                    ForwardDirection = Vector2Int.right
                },
                new BattleSpawnZone
                {
                    Side = BattleSpawnSide.Defender,
                    Area = new RectInt(
                        config.Width - config.DefenderSpawnColumns,
                        0,
                        config.DefenderSpawnColumns,
                        config.Height),
                    ForwardDirection = Vector2Int.left
                }
            };
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

        private void ValidateTileSet(BiomeConfig biomeConfig)
        {
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
            ValidateCoverConfig(config.DefaultCover, nameof(config.DefaultCover));
            ValidateCoverConfig(config.SettlementCover, nameof(config.SettlementCover));
            ValidateSignedByteConfig(config.MaxTileHeight, nameof(config.MaxTileHeight));

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

            ValidateSpawnLayoutConfig(config);
            ValidatePatchConfig(config.PlainsCoverPatchCount, config.PlainsCoverPatchRadius, nameof(config.PlainsCoverPatchCount));
            ValidatePatchConfig(config.ForestCoverPatchCount, config.ForestCoverPatchRadius, nameof(config.ForestCoverPatchCount));
            ValidatePatchConfig(config.ForestObstaclePatchCount, config.ForestObstaclePatchRadius, nameof(config.ForestObstaclePatchCount));
        }

        private static void ValidateByteConfig(int value, string configName)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                throw new InvalidOperationException($"{configName} must fit into byte range.");
            }
        }

        private static void ValidateSignedByteConfig(int value, string configName)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue)
            {
                throw new InvalidOperationException($"{configName} must fit into signed byte range.");
            }
        }

        private static void ValidateCoverConfig(int value, string configName)
        {
            if (value < (int)CoverType.None || value > (int)CoverType.Heavy)
            {
                throw new InvalidOperationException($"{configName} must map to a supported CoverType.");
            }
        }

        private static void ValidatePatchConfig(int count, int radius, string configName)
        {
            if (count < 0)
            {
                throw new InvalidOperationException($"{configName} cannot be negative.");
            }

            if (radius < 0)
            {
                throw new InvalidOperationException($"{configName} radius cannot be negative.");
            }

            if (count > 0 && radius <= 0)
            {
                throw new InvalidOperationException($"{configName} radius must be positive when patch count is positive.");
            }
        }

        private static void ValidateSpawnLayoutConfig(BattleMapGenerationConfig config)
        {
            var offset = config.UnitSpawnOffset;
            if (float.IsNaN(offset.x) ||
                float.IsNaN(offset.y) ||
                float.IsInfinity(offset.x) ||
                float.IsInfinity(offset.y) ||
                offset.x < 0f ||
                offset.x > 1f ||
                offset.y < 0f ||
                offset.y > 1f)
            {
                throw new InvalidOperationException("Battle unit spawn offset must stay inside a tile.");
            }

            if (float.IsNaN(config.UnitSpawnJitterRadius) ||
                float.IsInfinity(config.UnitSpawnJitterRadius) ||
                config.UnitSpawnJitterRadius < 0f ||
                config.UnitSpawnJitterRadius > MaxSpawnJitterRadius)
            {
                throw new InvalidOperationException("Battle unit spawn jitter radius must stay between zero and half a tile.");
            }
        }

        private static byte ToByte(int value, string configName)
        {
            ValidateByteConfig(value, configName);
            return (byte)value;
        }

        private static sbyte ToSByte(int value, string configName)
        {
            ValidateSignedByteConfig(value, configName);
            return (sbyte)value;
        }

        private static CoverType ToCoverType(int value, string configName)
        {
            ValidateCoverConfig(value, configName);
            return (CoverType)value;
        }
    }
}
