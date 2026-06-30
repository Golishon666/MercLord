using System;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    internal sealed class WorldGenerationConfigSnapshot
    {
        public readonly FactionGenerationConfig[] Factions;
        public readonly BiomeGenerationConfig[] Biomes;
        public readonly WorldTerrainGenerationSettings TerrainSettings;
        public readonly WorldNoiseSettings NoiseSettings;
        public readonly WorldRiverGenerationSettings RiverSettings;
        public readonly WorldRoadGenerationSettings RoadSettings;
        public readonly WorldFactionRegionGenerationSettings FactionRegionSettings;
        public readonly int StartingDay;
        public readonly int PlayerStartCellId;
        public readonly int PlayerStartingCredits;
        public readonly float StartingDominantInfluence;

        private WorldGenerationConfigSnapshot(
            FactionGenerationConfig[] factions,
            BiomeGenerationConfig[] biomes,
            WorldTerrainGenerationSettings terrainSettings,
            WorldNoiseSettings noiseSettings,
            WorldRiverGenerationSettings riverSettings,
            WorldRoadGenerationSettings roadSettings,
            WorldFactionRegionGenerationSettings factionRegionSettings,
            int startingDay,
            int playerStartCellId,
            int playerStartingCredits,
            float startingDominantInfluence)
        {
            Factions = factions ?? Array.Empty<FactionGenerationConfig>();
            Biomes = biomes ?? Array.Empty<BiomeGenerationConfig>();
            TerrainSettings = terrainSettings ?? WorldTerrainGenerationSettings.Default;
            NoiseSettings = noiseSettings ?? WorldNoiseSettings.Default;
            RiverSettings = riverSettings ?? WorldRiverGenerationSettings.Default;
            RoadSettings = roadSettings ?? WorldRoadGenerationSettings.Default;
            FactionRegionSettings = factionRegionSettings ?? WorldFactionRegionGenerationSettings.Default;
            StartingDay = startingDay;
            PlayerStartCellId = playerStartCellId;
            PlayerStartingCredits = playerStartingCredits;
            StartingDominantInfluence = startingDominantInfluence;
        }

        public static WorldGenerationConfigSnapshot From(ConfigDatabase configDatabase)
        {
            var globalGeneration = configDatabase?.GlobalGeneration;
            return new WorldGenerationConfigSnapshot(
                CopyFactions(configDatabase),
                CopyBiomes(configDatabase),
                globalGeneration?.Terrain ?? WorldTerrainGenerationSettings.Default,
                globalGeneration?.Noise ?? WorldNoiseSettings.Default,
                globalGeneration?.Rivers ?? WorldRiverGenerationSettings.Default,
                globalGeneration?.Roads ?? WorldRoadGenerationSettings.Default,
                globalGeneration?.FactionRegions ?? WorldFactionRegionGenerationSettings.Default,
                globalGeneration?.StartingDay ?? GlobalGenerationConfig.DefaultStartingDay,
                globalGeneration?.PlayerStartCellId ?? WorldIds.None,
                globalGeneration?.PlayerStartingCredits ?? GlobalGenerationConfig.DefaultPlayerStartingCredits,
                globalGeneration?.StartingDominantInfluence ?? GlobalGenerationConfig.DefaultStartingDominantInfluence);
        }

        private static FactionGenerationConfig[] CopyFactions(ConfigDatabase configDatabase)
        {
            var configuredFactions = configDatabase?.Factions;
            if (configuredFactions == null || configuredFactions.Count == 0)
            {
                return Array.Empty<FactionGenerationConfig>();
            }

            var result = new FactionGenerationConfig[configuredFactions.Count];
            for (var factionIndex = 0; factionIndex < configuredFactions.Count; factionIndex++)
            {
                var faction = configuredFactions[factionIndex];
                result[factionIndex] = faction != null
                    ? new FactionGenerationConfig(
                        faction.Id,
                        faction.StartingCredits,
                        faction.StartingStrength,
                        faction.CapitalCellId)
                    : default;
            }

            return result;
        }

        private static BiomeGenerationConfig[] CopyBiomes(ConfigDatabase configDatabase)
        {
            var configuredBiomes = configDatabase?.Biomes;
            if (configuredBiomes == null || configuredBiomes.Count == 0)
            {
                return Array.Empty<BiomeGenerationConfig>();
            }

            var result = new BiomeGenerationConfig[configuredBiomes.Count];
            for (var biomeIndex = 0; biomeIndex < configuredBiomes.Count; biomeIndex++)
            {
                var biome = configuredBiomes[biomeIndex];
                result[biomeIndex] = biome != null
                    ? new BiomeGenerationConfig(biome.BiomeType, biome.IsPassableByDefault)
                    : default;
            }

            return result;
        }
    }

    internal readonly struct FactionGenerationConfig
    {
        public readonly int Id;
        public readonly int StartingCredits;
        public readonly int StartingStrength;
        public readonly int CapitalCellId;
        public readonly bool IsConfigured;

        public FactionGenerationConfig(
            int id,
            int startingCredits,
            int startingStrength,
            int capitalCellId)
        {
            Id = id;
            StartingCredits = startingCredits;
            StartingStrength = startingStrength;
            CapitalCellId = capitalCellId;
            IsConfigured = true;
        }
    }

    internal readonly struct BiomeGenerationConfig
    {
        public readonly BiomeType BiomeType;
        public readonly bool IsPassableByDefault;
        public readonly bool IsConfigured;

        public BiomeGenerationConfig(BiomeType biomeType, bool isPassableByDefault)
        {
            BiomeType = biomeType;
            IsPassableByDefault = isPassableByDefault;
            IsConfigured = true;
        }
    }
}
