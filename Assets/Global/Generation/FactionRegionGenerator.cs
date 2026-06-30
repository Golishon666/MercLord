using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    internal sealed class FactionRegionGenerator
    {
        private readonly FactionGenerationConfig[] configuredFactions;
        private readonly IInfluenceService influenceService;
        private readonly float startingDominantInfluence;
        private readonly WorldFactionRegionGenerationSettings factionSettings;
        private readonly WorldTerrainGenerationSettings terrainSettings;

        public FactionRegionGenerator(
            FactionGenerationConfig[] configuredFactions,
            IInfluenceService influenceService,
            float startingDominantInfluence,
            WorldFactionRegionGenerationSettings factionSettings = null,
            WorldTerrainGenerationSettings terrainSettings = null)
        {
            this.configuredFactions = configuredFactions ?? Array.Empty<FactionGenerationConfig>();
            this.influenceService = influenceService;
            this.startingDominantInfluence = startingDominantInfluence;
            this.factionSettings = factionSettings ?? WorldFactionRegionGenerationSettings.Default;
            this.terrainSettings = terrainSettings ?? WorldTerrainGenerationSettings.Default;
        }

        public FactionData[] BuildFactionsAndApplyControl(
            WorldGenerationRequest request,
            WorldCell[] cells,
            WorldSpherePoint[] positions,
            IReadOnlyList<int>[] neighbours)
        {
            var factions = BuildFactions(request, cells, positions, out var regionCenters);
            ApplyFactionControl(cells, factions, neighbours);
            return factions;
        }

        private FactionData[] BuildFactions(
            WorldGenerationRequest request,
            WorldCell[] cells,
            WorldSpherePoint[] positions,
            out WorldSpherePoint[] factionRegionCenters)
        {
            var configuredCount = configuredFactions.Length;
            var factionCount = configuredCount > 0
                ? configuredCount
                : factionSettings.FallbackFactionCount;
            factionRegionCenters = CreateFactionRegionCenters(request.Seed, factionCount);
            var capitalCells = PickCapitalCells(request.Seed, cells, positions, factionRegionCenters);
            var factions = new FactionData[factionCount];

            for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
            {
                var hasConfiguredFaction = configuredCount > factionSlot && configuredFactions[factionSlot].IsConfigured;
                var configuredFaction = hasConfiguredFaction ? configuredFactions[factionSlot] : default;
                var factionId = hasConfiguredFaction ? configuredFaction.Id : factionSlot + 1;
                var capitalCellId = hasConfiguredFaction && IsValidCell(configuredFaction.CapitalCellId, cells.Length)
                    ? FindNearestPassableCell(configuredFaction.CapitalCellId, cells, positions)
                    : capitalCells[factionSlot];

                factions[factionSlot] = new FactionData
                {
                    Id = factionId,
                    Credits = hasConfiguredFaction ? configuredFaction.StartingCredits : factionSettings.FallbackFactionCredits,
                    Strength = hasConfiguredFaction ? configuredFaction.StartingStrength : factionSettings.FallbackFactionStrength,
                    CapitalCellId = capitalCellId
                };
            }

            return factions;
        }

        private void ApplyFactionControl(
            WorldCell[] cells,
            FactionData[] factions,
            IReadOnlyList<int>[] neighbours)
        {
            var factionSlots = BuildBalancedFactionSlots(cells, factions, neighbours);
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                ApplyFactionControlToCell(cells, factions, cellIndex, factionSlots[cellIndex]);
            }
        }

        private int[] BuildBalancedFactionSlots(
            WorldCell[] cells,
            FactionData[] factions,
            IReadOnlyList<int>[] neighbours)
        {
            var factionCount = factions.Length;
            var result = new int[cells.Length];
            for (var cellIndex = 0; cellIndex < result.Length; cellIndex++)
            {
                result[cellIndex] = WorldIds.None;
            }

            if (factionCount == 0 || cells.Length == 0)
            {
                return result;
            }

            var targetCounts = BuildTargetCounts(cells.Length, factionCount);
            var counts = new int[factionCount];
            var frontiers = new Queue<int>[factionCount];
            for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
            {
                frontiers[factionSlot] = new Queue<int>();
                var capitalCellId = factions[factionSlot].CapitalCellId;
                var seedCellId = FindSeedCell(cells, factions, factionSlot, result, capitalCellId);
                AssignFactionSlot(result, counts, frontiers, seedCellId, factionSlot);
            }

            var unassignedCount = cells.Length;
            for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
            {
                unassignedCount -= counts[factionSlot];
            }

            while (unassignedCount > 0)
            {
                var progressed = false;
                for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
                {
                    if (counts[factionSlot] >= targetCounts[factionSlot])
                    {
                        continue;
                    }

                    if (TryExpandFactionSlot(result, counts, frontiers, neighbours, factionSlot))
                    {
                        unassignedCount--;
                        progressed = true;
                    }
                }

                if (progressed)
                {
                    continue;
                }

                for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
                {
                    if (TryExpandFactionSlot(result, counts, frontiers, neighbours, factionSlot))
                    {
                        unassignedCount--;
                        progressed = true;
                    }
                }

                if (!progressed)
                {
                    unassignedCount -= AssignRemainingCellsByNearestCapital(cells, factions, result, counts);
                }
            }

            return result;
        }

        private static int[] BuildTargetCounts(int cellCount, int factionCount)
        {
            var targetCounts = new int[factionCount];
            var baseCount = cellCount / factionCount;
            var remainder = cellCount % factionCount;
            for (var factionSlot = 0; factionSlot < factionCount; factionSlot++)
            {
                targetCounts[factionSlot] = baseCount + (factionSlot < remainder ? 1 : 0);
            }

            return targetCounts;
        }

        private static int FindSeedCell(
            WorldCell[] cells,
            FactionData[] factions,
            int factionSlot,
            int[] assignedSlots,
            int preferredCellId)
        {
            if (IsValidCell(preferredCellId, cells.Length) && assignedSlots[preferredCellId] == WorldIds.None)
            {
                return preferredCellId;
            }

            var bestCell = WorldIds.None;
            var bestDot = float.MinValue;
            var preferredPosition = IsValidCell(preferredCellId, cells.Length)
                ? cells[preferredCellId].SpherePosition
                : factions[factionSlot].CapitalCellId >= 0 && factions[factionSlot].CapitalCellId < cells.Length
                    ? cells[factions[factionSlot].CapitalCellId].SpherePosition
                    : cells[0].SpherePosition;

            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (assignedSlots[cellIndex] != WorldIds.None)
                {
                    continue;
                }

                var dot = SphericalWorldGeometry.Dot(preferredPosition, cells[cellIndex].SpherePosition);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestCell = cellIndex;
                }
            }

            return bestCell == WorldIds.None ? 0 : bestCell;
        }

        private static void AssignFactionSlot(
            int[] assignedSlots,
            int[] counts,
            Queue<int>[] frontiers,
            int cellId,
            int factionSlot)
        {
            if (!IsValidCell(cellId, assignedSlots.Length) || assignedSlots[cellId] != WorldIds.None)
            {
                return;
            }

            assignedSlots[cellId] = factionSlot;
            counts[factionSlot]++;
            frontiers[factionSlot].Enqueue(cellId);
        }

        private static bool TryExpandFactionSlot(
            int[] assignedSlots,
            int[] counts,
            Queue<int>[] frontiers,
            IReadOnlyList<int>[] neighbours,
            int factionSlot)
        {
            var frontier = frontiers[factionSlot];
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (!IsValidCell(current, neighbours.Length))
                {
                    continue;
                }

                var currentNeighbours = neighbours[current];
                for (var neighbourIndex = 0; neighbourIndex < currentNeighbours.Count; neighbourIndex++)
                {
                    var neighbour = currentNeighbours[neighbourIndex];
                    if (!IsValidCell(neighbour, assignedSlots.Length) ||
                        assignedSlots[neighbour] != WorldIds.None)
                    {
                        continue;
                    }

                    assignedSlots[neighbour] = factionSlot;
                    counts[factionSlot]++;
                    frontier.Enqueue(current);
                    frontier.Enqueue(neighbour);
                    return true;
                }
            }

            return false;
        }

        private static int AssignRemainingCellsByNearestCapital(
            WorldCell[] cells,
            FactionData[] factions,
            int[] assignedSlots,
            int[] counts)
        {
            var assignedCount = 0;
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (assignedSlots[cellIndex] != WorldIds.None)
                {
                    continue;
                }

                var factionSlot = FindNearestCapitalSlot(cells, factions, cellIndex);
                assignedSlots[cellIndex] = factionSlot;
                counts[factionSlot]++;
                assignedCount++;
            }

            return assignedCount;
        }

        private static int FindNearestCapitalSlot(WorldCell[] cells, FactionData[] factions, int cellIndex)
        {
            var bestSlot = 0;
            var bestDot = float.MinValue;
            for (var factionSlot = 0; factionSlot < factions.Length; factionSlot++)
            {
                var capitalCellId = factions[factionSlot].CapitalCellId;
                if (!IsValidCell(capitalCellId, cells.Length))
                {
                    continue;
                }

                var dot = SphericalWorldGeometry.Dot(cells[cellIndex].SpherePosition, cells[capitalCellId].SpherePosition);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestSlot = factionSlot;
                }
            }

            return bestSlot;
        }

        private void ApplyFactionControlToCell(
            WorldCell[] cells,
            FactionData[] factions,
            int cellIndex,
            int factionSlot)
        {
            if (factionSlot == WorldIds.None)
            {
                factionSlot = 0;
            }

            var faction = factions[factionSlot];
            var influence = influenceService.CreateSingleFactionInfluence(
                factionSlot,
                startingDominantInfluence,
                factions.Length);

            cells[cellIndex].RegionId = faction.Id;
            cells[cellIndex].OwnerFactionId = faction.Id;
            cells[cellIndex].DominantFactionId = faction.Id;
            cells[cellIndex].Influence = influence;
        }

        private int[] PickCapitalCells(
            int seed,
            WorldCell[] cells,
            WorldSpherePoint[] positions,
            WorldSpherePoint[] factionRegionCenters)
        {
            var preferredCells = new List<int>();
            var fallbackCells = new List<int>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!cells[cellIndex].IsPassable)
                {
                    continue;
                }

                fallbackCells.Add(cellIndex);
                if (cells[cellIndex].Height > terrainSettings.CoastThreshold &&
                    cells[cellIndex].Height < factionSettings.CapitalMaxHeight)
                {
                    preferredCells.Add(cellIndex);
                }
            }

            if (fallbackCells.Count == 0)
            {
                throw new InvalidOperationException("World generation could not find passable land for faction capitals.");
            }

            var candidateCells = preferredCells.Count > 0 ? preferredCells : fallbackCells;
            var count = factionRegionCenters.Length;
            var capitals = new int[count];
            var usedCells = new HashSet<int>();
            for (var capitalIndex = 0; capitalIndex < count; capitalIndex++)
            {
                var bestCell = WorldIds.None;
                var bestDistance = float.MinValue;
                for (var candidateIndex = 0; candidateIndex < candidateCells.Count; candidateIndex++)
                {
                    var candidate = candidateCells[candidateIndex];
                    if (usedCells.Contains(candidate))
                    {
                        continue;
                    }

                    var regionAffinity = SphericalWorldGeometry.Dot(positions[candidate], factionRegionCenters[capitalIndex]);
                    var heightSuitability = 1f - Math.Abs(cells[candidate].Height - factionSettings.CapitalTargetHeight);
                    var jitter = SphericalWorldNoise.Hash01(seed, candidate, 307 + capitalIndex) * factionSettings.CapitalJitter;
                    var score = regionAffinity + heightSuitability * factionSettings.CapitalHeightSuitabilityWeight + jitter;
                    if (score > bestDistance)
                    {
                        bestDistance = score;
                        bestCell = candidate;
                    }
                }

                if (bestCell == WorldIds.None)
                {
                    bestCell = fallbackCells[Math.Abs(seed + capitalIndex * 92821) % fallbackCells.Count];
                }

                capitals[capitalIndex] = bestCell;
                usedCells.Add(bestCell);
            }

            return capitals;
        }

        private static WorldSpherePoint[] CreateFactionRegionCenters(int seed, int count)
        {
            var centers = new WorldSpherePoint[count];
            var goldenAngle = Math.PI * (3.0 - Math.Sqrt(5.0));
            var longitudeOffset = SphericalWorldNoise.Hash01(seed, count, 1301) * Math.PI * 2.0;
            var axis = SphericalWorldGeometry.Normalize(new WorldSpherePoint(
                SphericalWorldNoise.Hash01(seed, count, 1409) * 2f - 1f,
                SphericalWorldNoise.Hash01(seed, count, 1423) * 2f - 1f,
                SphericalWorldNoise.Hash01(seed, count, 1427) * 2f - 1f));
            var rotationAngle = SphericalWorldNoise.Hash01(seed, count, 1439) * Math.PI * 2.0;

            for (var index = 0; index < count; index++)
            {
                var t = (index + 0.5) / count;
                var y = 1.0 - t * 2.0;
                var radius = Math.Sqrt(Math.Max(0.0, 1.0 - y * y));
                var theta = goldenAngle * index + longitudeOffset;
                var point = new WorldSpherePoint(
                    (float)(Math.Cos(theta) * radius),
                    (float)y,
                    (float)(Math.Sin(theta) * radius));
                centers[index] = SphericalWorldGeometry.RotateAroundAxis(point, axis, rotationAngle);
            }

            return centers;
        }

        private static int FindNearestPassableCell(int sourceCellId, WorldCell[] cells, WorldSpherePoint[] positions)
        {
            if (cells[sourceCellId].IsPassable)
            {
                return sourceCellId;
            }

            var bestCell = WorldIds.None;
            var bestDot = float.MinValue;
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!cells[cellIndex].IsPassable)
                {
                    continue;
                }

                var dot = SphericalWorldGeometry.Dot(positions[sourceCellId], positions[cellIndex]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestCell = cellIndex;
                }
            }

            return bestCell == WorldIds.None ? sourceCellId : bestCell;
        }

        private static bool IsValidCell(int cellId, int cellCount)
        {
            return cellId >= 0 && cellId < cellCount;
        }
    }
}
