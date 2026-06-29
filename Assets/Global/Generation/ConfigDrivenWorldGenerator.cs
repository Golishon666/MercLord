using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    public sealed class ConfigDrivenWorldGenerator : IWorldGenerator
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IInfluenceService influenceService;

        public ConfigDrivenWorldGenerator(ConfigDatabase configDatabase, IInfluenceService influenceService)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.influenceService = influenceService ?? throw new ArgumentNullException(nameof(influenceService));
        }

        public WorldModel Generate(WorldGenerationRequest request)
        {
            ValidateRequest(request);
            var globalConfig = configDatabase.GlobalGeneration;
            var factionConfigs = configDatabase.Factions;
            var factionCount = factionConfigs.Count;
            var cells = new WorldCell[request.TargetCellCount];
            var neighbours = new CellNeighbours[request.TargetCellCount];

            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var biomeConfig = configDatabase.Biomes[cellIndex % configDatabase.Biomes.Count];
                var dominantFactionSlot = cellIndex % factionCount;
                var influence = influenceService.CreateSingleFactionInfluence(
                    dominantFactionSlot,
                    globalConfig.StartingDominantInfluence);
                var ownerFactionId = factionConfigs[dominantFactionSlot].Id;
                var dominantFactionId = factionConfigs[influenceService.GetDominantFactionSlot(influence)].Id;

                cells[cellIndex] = new WorldCell
                {
                    Id = cellIndex,
                    Biome = biomeConfig.BiomeType,
                    RegionId = ownerFactionId,
                    Height = globalConfig.DefaultHeight,
                    Moisture = globalConfig.DefaultMoisture,
                    Temperature = globalConfig.DefaultTemperature,
                    OwnerFactionId = ownerFactionId,
                    DominantFactionId = dominantFactionId,
                    Influence = influence,
                    SettlementId = WorldIds.None,
                    HasRoad = HasRoad(cellIndex, globalConfig.RoadStride),
                    IsPassable = biomeConfig.IsPassableByDefault
                };

                neighbours[cellIndex] = BuildLinearNeighbours(cellIndex, cells.Length);
            }

            return new WorldModel
            {
                Seed = request.Seed,
                CurrentDay = globalConfig.StartingDay,
                Cells = cells,
                Neighbours = neighbours,
                Factions = BuildFactions(),
                Settlements = Array.Empty<SettlementData>(),
                Armies = Array.Empty<ArmyData>(),
                Player = new PlayerGlobalData
                {
                    CultureId = WorldIds.None,
                    FactionId = WorldIds.None,
                    CellId = globalConfig.PlayerStartCellId,
                    Credits = globalConfig.PlayerStartingCredits,
                    Party = Array.Empty<SquadData>()
                }
            };
        }

        private void ValidateRequest(WorldGenerationRequest request)
        {
            if (request.TargetCellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "World target cell count must be positive.");
            }

            if (configDatabase.GlobalGeneration == null)
            {
                throw new InvalidOperationException("GlobalGenerationConfig is required.");
            }

            if (configDatabase.GlobalGeneration.PlayerStartCellId < 0 ||
                configDatabase.GlobalGeneration.PlayerStartCellId >= request.TargetCellCount)
            {
                throw new InvalidOperationException("Player start cell id must point to a generated world cell.");
            }

            if (configDatabase.GlobalGeneration.StartingDominantInfluence <= 0f)
            {
                throw new InvalidOperationException("Starting dominant influence must be positive.");
            }

            ValidateBiomeConfigs();
            ValidateFactionConfigs(request.TargetCellCount);
        }

        private void ValidateBiomeConfigs()
        {
            if (configDatabase.Biomes == null || configDatabase.Biomes.Count == 0)
            {
                throw new InvalidOperationException("At least one BiomeConfig is required.");
            }

            for (var biomeIndex = 0; biomeIndex < configDatabase.Biomes.Count; biomeIndex++)
            {
                if (configDatabase.Biomes[biomeIndex] == null)
                {
                    throw new InvalidOperationException($"Biome config slot {biomeIndex} is empty.");
                }
            }
        }

        private void ValidateFactionConfigs(int cellCount)
        {
            if (configDatabase.Factions == null || configDatabase.Factions.Count == 0)
            {
                throw new InvalidOperationException("At least one FactionConfig is required.");
            }

            if (configDatabase.Factions.Count > Influence4.Capacity)
            {
                throw new InvalidOperationException($"Influence4 supports up to {Influence4.Capacity} configured factions.");
            }

            var usedFactionIds = new HashSet<int>();
            for (var factionSlot = 0; factionSlot < configDatabase.Factions.Count; factionSlot++)
            {
                var factionConfig = configDatabase.Factions[factionSlot];
                if (factionConfig == null)
                {
                    throw new InvalidOperationException($"Faction config slot {factionSlot} is empty.");
                }

                if (!usedFactionIds.Add(factionConfig.Id))
                {
                    throw new InvalidOperationException($"Faction id {factionConfig.Id} is duplicated.");
                }

                if (factionConfig.StartingCredits < 0 || factionConfig.StartingStrength < 0)
                {
                    throw new InvalidOperationException($"{factionConfig.DisplayName} has negative starting faction values.");
                }

                if (factionConfig.CapitalCellId < 0 || factionConfig.CapitalCellId >= cellCount)
                {
                    throw new InvalidOperationException($"{factionConfig.DisplayName} capital cell id must point to a generated world cell.");
                }
            }
        }

        private FactionData[] BuildFactions()
        {
            var factionConfigs = configDatabase.Factions;
            var factions = new FactionData[factionConfigs.Count];
            for (var factionSlot = 0; factionSlot < factions.Length; factionSlot++)
            {
                var factionConfig = factionConfigs[factionSlot];
                factions[factionSlot] = new FactionData
                {
                    Id = factionConfig.Id,
                    Credits = factionConfig.StartingCredits,
                    CapitalCellId = factionConfig.CapitalCellId,
                    Strength = factionConfig.StartingStrength
                };
            }

            return factions;
        }

        private static CellNeighbours BuildLinearNeighbours(int cellIndex, int cellCount)
        {
            return new CellNeighbours
            {
                N0 = cellIndex > 0 ? cellIndex - 1 : WorldIds.None,
                N1 = cellIndex < cellCount - 1 ? cellIndex + 1 : WorldIds.None,
                N2 = WorldIds.None,
                N3 = WorldIds.None,
                N4 = WorldIds.None,
                N5 = WorldIds.None
            };
        }

        private static bool HasRoad(int cellIndex, int roadStride)
        {
            return roadStride > 0 && cellIndex % roadStride == 0;
        }
    }
}
