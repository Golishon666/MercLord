using System;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    internal sealed class SphericalTerrainGenerator
    {
        private readonly BiomeGenerationConfig[] configuredBiomes;
        private readonly WorldTerrainGenerationSettings terrainSettings;
        private readonly WorldNoiseSettings noiseSettings;

        public SphericalTerrainGenerator(
            BiomeGenerationConfig[] configuredBiomes,
            WorldTerrainGenerationSettings terrainSettings = null,
            WorldNoiseSettings noiseSettings = null)
        {
            this.configuredBiomes = configuredBiomes ?? Array.Empty<BiomeGenerationConfig>();
            this.terrainSettings = terrainSettings ?? WorldTerrainGenerationSettings.Default;
            this.noiseSettings = noiseSettings ?? WorldNoiseSettings.Default;
        }

        public WorldCell[] BuildCells(WorldGenerationRequest request, WorldSpherePoint[] positions)
        {
            var samples = new TerrainSample[positions.Length];
            for (var cellIndex = 0; cellIndex < samples.Length; cellIndex++)
            {
                var point = positions[cellIndex];
                var climateLatitude = GetWarpedClimateLatitude(point, request.Seed);
                var continent = SphericalWorldNoise.Noise(
                    point,
                    request.Seed,
                    terrainSettings.ContinentSalt,
                    terrainSettings.ContinentOctaves,
                    terrainSettings.ContinentFrequency,
                    noiseSettings);
                var detail = SphericalWorldNoise.Noise(
                    point,
                    request.Seed,
                    terrainSettings.DetailSalt,
                    terrainSettings.DetailOctaves,
                    terrainSettings.DetailFrequency,
                    noiseSettings);
                var ridge = 1f - Math.Abs(SphericalWorldNoise.Noise(
                    point,
                    request.Seed,
                    terrainSettings.RidgeSalt,
                    terrainSettings.RidgeOctaves,
                    terrainSettings.RidgeFrequency,
                    noiseSettings) * 2f - 1f);
                var rawHeight =
                    continent * terrainSettings.ContinentWeight +
                    detail * terrainSettings.DetailWeight +
                    ridge * terrainSettings.RidgeWeight;
                var height = Clamp01((rawHeight - terrainSettings.HeightContrastPivot) * terrainSettings.HeightContrast + terrainSettings.HeightOffset);
                var moistureNoise = SphericalWorldNoise.Noise(
                    point,
                    request.Seed,
                    terrainSettings.MoistureSalt,
                    terrainSettings.MoistureOctaves,
                    terrainSettings.MoistureFrequency,
                    noiseSettings);
                var moisture = Clamp01(moistureNoise * terrainSettings.MoistureNoiseWeight + climateLatitude * terrainSettings.MoistureLatitudeWeight);
                var temperatureNoise = SphericalWorldNoise.Noise(
                    point,
                    request.Seed,
                    terrainSettings.TemperatureSalt,
                    terrainSettings.TemperatureOctaves,
                    terrainSettings.TemperatureFrequency,
                    noiseSettings);
                var temperature = Clamp01(
                    terrainSettings.TemperatureBase -
                    climateLatitude * terrainSettings.TemperatureLatitudeWeight -
                    height * terrainSettings.TemperatureHeightWeight +
                    temperatureNoise * terrainSettings.TemperatureNoiseWeight);

                samples[cellIndex] = new TerrainSample(point, height, moisture, temperature, ridge);
            }

            var oceanThreshold = ResolveHeightPercentile(samples, terrainSettings.TargetWaterCoverage, terrainSettings.OceanThreshold);
            var coastThreshold = ResolveHeightPercentile(
                samples,
                Math.Min(0.95f, terrainSettings.TargetWaterCoverage + terrainSettings.CoastCoverage),
                terrainSettings.CoastThreshold);
            if (coastThreshold <= oceanThreshold)
            {
                coastThreshold = Math.Min(1f, oceanThreshold + 0.025f);
            }

            var cells = new WorldCell[positions.Length];
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var sample = samples[cellIndex];
                var biome = PickBiome(
                    sample.Height,
                    sample.Moisture,
                    sample.Temperature,
                    sample.Ridge,
                    oceanThreshold,
                    coastThreshold,
                    request.Seed,
                    cellIndex);
                var isPassable = IsPassableBiome(biome) && !IsImpassableMountain(biome, sample.Height);

                cells[cellIndex] = new WorldCell
                {
                    Id = cellIndex,
                    SpherePosition = sample.Point,
                    Biome = biome,
                    RegionId = WorldIds.None,
                    Height = sample.Height,
                    Moisture = sample.Moisture,
                    Temperature = sample.Temperature,
                    OwnerFactionId = WorldIds.None,
                    DominantFactionId = WorldIds.None,
                    ResourceAmount = CalculateResourceAmount(biome, sample.Height, sample.Moisture, request.Seed, cellIndex),
                    Influence = default,
                    SettlementId = WorldIds.None,
                    HasRoad = false,
                    RoadType = RoadType.None,
                    HasRiver = false,
                    RiverFlow = 0f,
                    DownstreamCellId = WorldIds.None,
                    DistanceToWater = int.MaxValue,
                    MovementCost = isPassable
                        ? WorldMovementCosts.Calculate(biome, RoadType.None, false)
                        : WorldMovementCosts.ImpassableCost,
                    IsPassable = isPassable
                };
            }

            return cells;
        }

        private float GetWarpedClimateLatitude(WorldSpherePoint point, int seed)
        {
            var latitudeWarp = SphericalWorldNoise.Noise(
                point,
                seed,
                terrainSettings.ClimateLatitudeWarpSalt,
                terrainSettings.ClimateLatitudeWarpOctaves,
                terrainSettings.ClimateLatitudeWarpFrequency,
                noiseSettings) - 0.5f;
            return Clamp01(Math.Abs(point.Y + latitudeWarp * terrainSettings.ClimateLatitudeWarpStrength));
        }

        internal static int CalculateResourceAmount(
            BiomeType biome,
            float height,
            float moisture,
            int seed,
            int cellIndex,
            float riverFlow = 0f,
            int distanceToWater = int.MaxValue)
        {
            if (biome == BiomeType.Ocean)
            {
                return 0;
            }

            var richness = SphericalWorldNoise.Hash01(seed, cellIndex, 1709);
            var baseAmount = biome switch
            {
                BiomeType.Coast => 8f,
                BiomeType.Plains => 28f,
                BiomeType.Forest => 36f,
                BiomeType.Desert => 16f,
                BiomeType.Snow => 14f,
                BiomeType.Swamp => 30f,
                BiomeType.Mountains => 54f,
                BiomeType.AshWastes => 22f,
                BiomeType.RustDesert => 32f,
                BiomeType.DeadForest => 24f,
                BiomeType.IndustrialRuins => 48f,
                BiomeType.DemonScar => 38f,
                BiomeType.ToxicSwamp => 34f,
                _ => 20f
            };

            var heightBonus = biome == BiomeType.Mountains || biome == BiomeType.RustDesert
                ? height * 24f
                : height * 10f;
            var moistureBonus = biome == BiomeType.Forest || biome == BiomeType.Swamp || biome == BiomeType.ToxicSwamp
                ? moisture * 16f
                : moisture * 6f;
            var waterBonus = 0f;
            if (riverFlow > 0f)
            {
                waterBonus += 14f + (float)Math.Sqrt(riverFlow) * 6f;
            }

            if (distanceToWater <= 1)
            {
                waterBonus += 10f;
            }
            else if (distanceToWater <= 3)
            {
                waterBonus += 5f;
            }

            return Math.Max(0, (int)Math.Round(baseAmount + heightBonus + moistureBonus + waterBonus + richness * 24f));
        }

        private BiomeType PickBiome(
            float height,
            float moisture,
            float temperature,
            float ridge,
            float oceanThreshold,
            float coastThreshold,
            int seed,
            int cellIndex)
        {
            if (height < oceanThreshold)
            {
                return BiomeType.Ocean;
            }

            if (height < coastThreshold)
            {
                return BiomeType.Coast;
            }

            if (height > terrainSettings.MountainHeightThreshold ||
                (height > terrainSettings.MountainRidgeMinHeight && ridge > terrainSettings.MountainRidgeThreshold))
            {
                return BiomeType.Mountains;
            }

            if (temperature < terrainSettings.SnowTemperatureThreshold)
            {
                return BiomeType.Snow;
            }

            if (height > terrainSettings.RustDesertMinHeight &&
                moisture < terrainSettings.RustDesertMaxMoisture &&
                temperature > terrainSettings.RustDesertMinTemperature &&
                SphericalWorldNoise.Hash01(seed, cellIndex, 701) > terrainSettings.RustDesertChanceThreshold)
            {
                return BiomeType.RustDesert;
            }

            if (height > terrainSettings.AshWastesMinHeight &&
                moisture < terrainSettings.AshWastesMaxMoisture &&
                SphericalWorldNoise.Hash01(seed, cellIndex, 709) > terrainSettings.AshWastesChanceThreshold)
            {
                return BiomeType.AshWastes;
            }

            if (moisture > terrainSettings.SwampMinMoisture && height < terrainSettings.SwampMaxHeight)
            {
                return temperature > terrainSettings.ToxicSwampMinTemperature &&
                       SphericalWorldNoise.Hash01(seed, cellIndex, 719) > terrainSettings.ToxicSwampChanceThreshold
                    ? BiomeType.ToxicSwamp
                    : BiomeType.Swamp;
            }

            if (moisture > terrainSettings.ForestMinMoisture)
            {
                return temperature < terrainSettings.DeadForestMaxTemperature &&
                       SphericalWorldNoise.Hash01(seed, cellIndex, 727) > terrainSettings.DeadForestChanceThreshold
                    ? BiomeType.DeadForest
                    : BiomeType.Forest;
            }

            if (moisture < terrainSettings.DesertMaxMoisture && temperature > terrainSettings.DesertMinTemperature)
            {
                return BiomeType.Desert;
            }

            if (height > terrainSettings.IndustrialRuinsMinHeight &&
                SphericalWorldNoise.Hash01(seed, cellIndex, 733) > terrainSettings.IndustrialRuinsChanceThreshold)
            {
                return BiomeType.IndustrialRuins;
            }

            if (height > terrainSettings.DemonScarMinHeight &&
                moisture < terrainSettings.DemonScarMaxMoisture &&
                SphericalWorldNoise.Hash01(seed, cellIndex, 739) > terrainSettings.DemonScarChanceThreshold)
            {
                return BiomeType.DemonScar;
            }

            return BiomeType.Plains;
        }

        private static float ResolveHeightPercentile(TerrainSample[] samples, float percentile, float fallback)
        {
            if (samples.Length == 0)
            {
                return fallback;
            }

            var heights = new float[samples.Length];
            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                heights[sampleIndex] = samples[sampleIndex].Height;
            }

            Array.Sort(heights);
            var clamped = Clamp01(percentile);
            var thresholdIndex = Math.Min(heights.Length - 1, Math.Max(0, (int)Math.Floor((heights.Length - 1) * clamped)));
            return heights[thresholdIndex];
        }

        private readonly struct TerrainSample
        {
            public readonly WorldSpherePoint Point;
            public readonly float Height;
            public readonly float Moisture;
            public readonly float Temperature;
            public readonly float Ridge;

            public TerrainSample(
                WorldSpherePoint point,
                float height,
                float moisture,
                float temperature,
                float ridge)
            {
                Point = point;
                Height = height;
                Moisture = moisture;
                Temperature = temperature;
                Ridge = ridge;
            }
        }

        private bool IsPassableBiome(BiomeType biome)
        {
            if (biome == BiomeType.Ocean)
            {
                return false;
            }

            for (var biomeIndex = 0; biomeIndex < configuredBiomes.Length; biomeIndex++)
            {
                var config = configuredBiomes[biomeIndex];
                if (config.IsConfigured && config.BiomeType == biome)
                {
                    return config.IsPassableByDefault;
                }
            }

            return true;
        }

        private bool IsImpassableMountain(BiomeType biome, float height)
        {
            return biome == BiomeType.Mountains && height >= terrainSettings.ImpassableMountainThreshold;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }
}
