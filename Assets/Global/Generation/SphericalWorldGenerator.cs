using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    public sealed class SphericalWorldGenerator : IWorldGenerator
    {
        private const int NeighbourCount = 6;
        private const int NeighbourCandidateMultiplier = 14;
        private const int IcosahedronVertexCount = 12;
        private const int IcosahedronFaceCount = 20;
        private const float MinimumNeighbourBucketSize = 0.025f;
        private const float MaximumNeighbourBucketSize = 0.25f;
        private const float NeighbourBucketSpacingMultiplier = 2.5f;

        private readonly WorldGenerationConfigSnapshot configSnapshot;
        private readonly IInfluenceService influenceService;
        private readonly WorldTerrainGenerationSettings terrainSettings;
        private readonly WorldNoiseSettings noiseSettings;
        private readonly WorldRiverGenerationSettings riverSettings;
        private readonly WorldRoadGenerationSettings roadSettings;
        private readonly WorldFactionRegionGenerationSettings factionRegionSettings;

        public SphericalWorldGenerator(ConfigDatabase configDatabase = null, IInfluenceService influenceService = null)
        {
            configSnapshot = WorldGenerationConfigSnapshot.From(configDatabase);
            this.influenceService = influenceService ?? new InfluenceService();
            terrainSettings = configSnapshot.TerrainSettings;
            noiseSettings = configSnapshot.NoiseSettings;
            riverSettings = configSnapshot.RiverSettings;
            roadSettings = configSnapshot.RoadSettings;
            factionRegionSettings = configSnapshot.FactionRegionSettings;
        }

        public WorldModel Generate(WorldGenerationRequest request)
        {
            return GenerateInternal(request, default);
        }

        public async UniTask<WorldModel> GenerateAsync(
            WorldGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            try
            {
                return GenerateInternal(request, cancellationToken);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        private WorldModel GenerateInternal(WorldGenerationRequest request, CancellationToken cancellationToken)
        {
            if (request.TargetCellCount <= NeighbourCount + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "World target cell count must be greater than seven.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            CreateGeodesicGrid(request.TargetCellCount, out var positions, out var neighbours);
            cancellationToken.ThrowIfCancellationRequested();

            var terrainGenerator = new SphericalTerrainGenerator(configSnapshot.Biomes, terrainSettings, noiseSettings);
            var cells = terrainGenerator.BuildCells(request, positions);
            cancellationToken.ThrowIfCancellationRequested();

            var factionGenerator = new FactionRegionGenerator(
                configSnapshot.Factions,
                influenceService,
                GetStartingDominantInfluence(),
                factionRegionSettings,
                terrainSettings);
            var factions = factionGenerator.BuildFactionsAndApplyControl(request, cells, positions, neighbours);
            cancellationToken.ThrowIfCancellationRequested();

            ApplyDistanceToWater(cells, CalculateDistanceToWater(cells, neighbours));
            var riverBuild = BuildRivers(request.Seed, cells, neighbours);
            ApplyRiverData(cells, riverBuild.Edges);
            ApplyHydrologyBiomesAndResources(request.Seed, cells);
            cancellationToken.ThrowIfCancellationRequested();

            var roadEdges = new Dictionary<ulong, RoadType>();
            var settlements = BuildSettlementsAndRoads(request.Seed, cells, neighbours, factions, roadEdges);
            var activities = BuildActivitiesAndSmallRoads(request.Seed, cells, neighbours, settlements, roadEdges);
            var roadEdgeArray = ApplyRoads(cells, roadEdges);
            ApplyMovementCosts(cells);
            cancellationToken.ThrowIfCancellationRequested();

            return new WorldModel
            {
                Seed = request.Seed,
                CurrentDay = GetStartingDay(),
                Cells = cells,
                Neighbours = ToCellNeighbours(neighbours),
                RoadEdges = roadEdgeArray,
                Rivers = riverBuild.Rivers,
                RiverEdges = riverBuild.Edges,
                Factions = factions,
                Settlements = settlements,
                Activities = activities,
                Armies = Array.Empty<ArmyData>(),
                Player = new PlayerGlobalData
                {
                    CultureId = WorldIds.None,
                    FactionId = WorldIds.None,
                    CellId = GetPlayerStartCell(cells, factions),
                    Credits = GetPlayerStartingCredits(),
                    Party = Array.Empty<SquadData>()
                }
            };
        }

        private SettlementData[] BuildSettlementsAndRoads(
            int seed,
            WorldCell[] cells,
            List<int>[] neighbours,
            FactionData[] factions,
            Dictionary<ulong, RoadType> roadEdges)
        {
            var settlements = new List<SettlementData>(factions.Length + roadSettings.TownMinCount);
            for (var factionSlot = 0; factionSlot < factions.Length; factionSlot++)
            {
                var cellId = factions[factionSlot].CapitalCellId;
                var settlement = new SettlementData
                {
                    Id = settlements.Count,
                    FactionId = factions[factionSlot].Id,
                    CellId = cellId
                };
                cells[cellId].SettlementId = settlement.Id;
                settlements.Add(settlement);
            }

            BuildLargeRoadNetwork(cells, neighbours, factions, roadEdges);

            var largeRoadCells = CollectRoadCells(roadEdges, RoadType.Large);
            var townCount = Math.Min(
                roadSettings.TownMaxCount,
                Math.Max(roadSettings.TownMinCount, cells.Length / roadSettings.TownCountDivisor));
            var townCells = PickNetworkSatellites(
                seed,
                cells,
                neighbours,
                largeRoadCells,
                townCount,
                ScaleGraphDistance(cells.Length, roadSettings.TownMinNetworkDistance, roadSettings),
                ScaleGraphDistance(cells.Length, roadSettings.TownMaxNetworkDistance, roadSettings),
                settlements,
                roadSettings,
                noiseSettings);
            for (var townIndex = 0; townIndex < townCells.Count; townIndex++)
            {
                var cellId = townCells[townIndex];
                var settlement = new SettlementData
                {
                    Id = settlements.Count,
                    FactionId = cells[cellId].OwnerFactionId,
                    CellId = cellId
                };
                cells[cellId].SettlementId = settlement.Id;
                settlements.Add(settlement);

                var path = FindPathToNetwork(cells, neighbours, cellId, largeRoadCells, roadSettings);
                AddRoadPath(path, RoadType.Medium, roadEdges);
            }

            return settlements.ToArray();
        }

        private WorldActivityData[] BuildActivitiesAndSmallRoads(
            int seed,
            WorldCell[] cells,
            List<int>[] neighbours,
            SettlementData[] settlements,
            Dictionary<ulong, RoadType> roadEdges)
        {
            var mediumRoadCells = CollectRoadCells(roadEdges, RoadType.Medium);
            if (mediumRoadCells.Count == 0)
            {
                mediumRoadCells = CollectRoadCells(roadEdges, RoadType.Large);
            }

            var activityCount = Math.Min(
                roadSettings.ActivityMaxCount,
                Math.Max(roadSettings.ActivityMinCount, cells.Length / roadSettings.ActivityCountDivisor));
            var selectedCells = PickNetworkSatellites(
                seed + 7919,
                cells,
                neighbours,
                mediumRoadCells,
                activityCount,
                ScaleGraphDistance(cells.Length, roadSettings.ActivityMinNetworkDistance, roadSettings),
                ScaleGraphDistance(cells.Length, roadSettings.ActivityMaxNetworkDistance, roadSettings),
                settlements,
                roadSettings,
                noiseSettings);
            var activities = new WorldActivityData[selectedCells.Count];
            for (var activityIndex = 0; activityIndex < selectedCells.Count; activityIndex++)
            {
                var cellId = selectedCells[activityIndex];
                activities[activityIndex] = new WorldActivityData
                {
                    Id = activityIndex,
                    FactionId = cells[cellId].OwnerFactionId,
                    CellId = cellId,
                    Type = (WorldActivityType)(activityIndex % Enum.GetValues(typeof(WorldActivityType)).Length)
                };

                var path = FindPathToNetwork(cells, neighbours, cellId, mediumRoadCells, roadSettings);
                AddRoadPath(path, RoadType.Small, roadEdges);
            }

            return activities;
        }

        private void BuildLargeRoadNetwork(
            WorldCell[] cells,
            List<int>[] neighbours,
            FactionData[] factions,
            Dictionary<ulong, RoadType> roadEdges)
        {
            if (factions.Length < 2)
            {
                return;
            }

            var selected = new bool[factions.Length];
            selected[0] = true;
            var selectedCount = 1;
            while (selectedCount < factions.Length)
            {
                var bestFrom = 0;
                var bestTo = 0;
                var bestDistance = float.MaxValue;
                for (var fromSlot = 0; fromSlot < factions.Length; fromSlot++)
                {
                    if (!selected[fromSlot])
                    {
                        continue;
                    }

                    for (var toSlot = 0; toSlot < factions.Length; toSlot++)
                    {
                        if (selected[toSlot])
                        {
                            continue;
                        }

                        var distance = 1f - SphericalWorldGeometry.Dot(
                            cells[factions[fromSlot].CapitalCellId].SpherePosition,
                            cells[factions[toSlot].CapitalCellId].SpherePosition);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestFrom = fromSlot;
                            bestTo = toSlot;
                        }
                    }
                }

                var path = FindPath(cells, neighbours, factions[bestFrom].CapitalCellId, factions[bestTo].CapitalCellId, roadSettings);
                AddRoadPath(path, RoadType.Large, roadEdges);
                selected[bestTo] = true;
                selectedCount++;
            }
        }

        private RiverBuildResult BuildRivers(int seed, WorldCell[] cells, List<int>[] neighbours)
        {
            var sources = new List<int>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (IsRiverSourceCandidate(cells[cellIndex], riverSettings))
                {
                    sources.Add(cellIndex);
                }
            }

            sources.Sort((left, right) =>
            {
                var leftScore = cells[left].Height + SphericalWorldNoise.Noise(
                    cells[left].SpherePosition,
                    seed,
                    riverSettings.SourceNoiseSalt,
                    riverSettings.SourceNoiseOctaves,
                    riverSettings.SourceNoiseFrequency,
                    noiseSettings) * riverSettings.SourceNoiseWeight;
                var rightScore = cells[right].Height + SphericalWorldNoise.Noise(
                    cells[right].SpherePosition,
                    seed,
                    riverSettings.SourceNoiseSalt,
                    riverSettings.SourceNoiseOctaves,
                    riverSettings.SourceNoiseFrequency,
                    noiseSettings) * riverSettings.SourceNoiseWeight;
                return rightScore.CompareTo(leftScore);
            });

            var riverCount = Math.Min(
                riverSettings.MaxRiverCount,
                Math.Max(riverSettings.MinRiverCount, cells.Length / riverSettings.RiverCountDivisor));
            var riverEdges = new List<WorldRiverEdge>(riverCount * riverSettings.ExpectedEdgesPerRiver);
            var rivers = new List<WorldRiverData>(riverCount);
            var edgeIndexes = new Dictionary<ulong, int>(riverCount * riverSettings.ExpectedEdgesPerRiver);
            var riverCells = new bool[cells.Length];
            var usedSources = new List<int>();

            for (var sourceIndex = 0; sourceIndex < sources.Count && rivers.Count < riverCount; sourceIndex++)
            {
                var source = sources[sourceIndex];
                if (!IsFarFromSelection(cells, source, usedSources, riverSettings.SourceMinAngularDistance))
                {
                    continue;
                }

                var path = FindRiverPathToWater(seed, source, cells, neighbours, riverCells, edgeIndexes);
                if (path == null || path.Count - 1 < riverSettings.MinRiverLength || !IsWaterCell(cells[path[path.Count - 1]]))
                {
                    continue;
                }

                var riverId = rivers.Count;
                var totalFlow = 0f;
                for (var pathIndex = 1; pathIndex < path.Count; pathIndex++)
                {
                    var from = path[pathIndex - 1];
                    var to = path[pathIndex];
                    var edgeFlow = riverSettings.FlowBase + (pathIndex - 1) * riverSettings.FlowPerStep;
                    var key = GetDirectedEdgeKey(from, to);
                    if (edgeIndexes.TryGetValue(key, out var existingEdgeIndex))
                    {
                        var edge = riverEdges[existingEdgeIndex];
                        edge.Flow += edgeFlow;
                        riverEdges[existingEdgeIndex] = edge;
                        totalFlow = Math.Max(totalFlow, edge.Flow);
                    }
                    else
                    {
                        edgeIndexes.Add(key, riverEdges.Count);
                        riverEdges.Add(new WorldRiverEdge
                        {
                            RiverId = riverId,
                            FromCellId = from,
                            ToCellId = to,
                            Flow = edgeFlow
                        });
                        totalFlow = Math.Max(totalFlow, edgeFlow);
                    }

                    riverCells[from] = true;
                    riverCells[to] = true;
                }

                rivers.Add(new WorldRiverData
                {
                    Id = riverId,
                    SourceCellId = source,
                    MouthCellId = path[path.Count - 1],
                    EdgeCount = path.Count - 1,
                    TotalFlow = totalFlow
                });
                usedSources.Add(source);
            }

            return new RiverBuildResult(rivers.ToArray(), riverEdges.ToArray());
        }

        private List<int> FindRiverPathToWater(
            int seed,
            int source,
            WorldCell[] cells,
            List<int>[] neighbours,
            bool[] riverCells,
            Dictionary<ulong, int> riverEdgeIndexes)
        {
            var count = cells.Length;
            var distances = new float[count];
            var previous = new int[count];
            var visited = new bool[count];
            for (var cellIndex = 0; cellIndex < count; cellIndex++)
            {
                distances[cellIndex] = float.MaxValue;
                previous[cellIndex] = WorldIds.None;
            }

            var maxVisited = Math.Min(
                count,
                Math.Max(riverSettings.MouthSearchMinVisitedCells, count / riverSettings.MouthSearchCellDivisor));
            distances[source] = 0f;
            var visitedCount = 0;
            var queue = new PathPriorityQueue(Math.Max(riverSettings.TraceInitialCapacity, maxVisited / 4));
            queue.Enqueue(source, 0f);

            while (queue.Count > 0 && visitedCount < maxVisited)
            {
                var node = queue.Dequeue();
                var current = node.CellId;
                if (visited[current])
                {
                    continue;
                }

                visited[current] = true;
                visitedCount++;
                if (current != source && IsWaterCell(cells[current]))
                {
                    return ReconstructPath(previous, current);
                }

                for (var neighbourIndex = 0; neighbourIndex < neighbours[current].Count; neighbourIndex++)
                {
                    var next = neighbours[current][neighbourIndex];
                    if (visited[next] || !IsRiverTraceable(cells[next]))
                    {
                        continue;
                    }

                    var edgeCost = GetRiverPathCost(
                        seed,
                        source,
                        cells[current],
                        cells[next],
                        riverCells[next],
                        riverEdgeIndexes.ContainsKey(GetDirectedEdgeKey(next, current)));
                    var candidateDistance = distances[current] + edgeCost;
                    if (candidateDistance >= distances[next])
                    {
                        continue;
                    }

                    distances[next] = candidateDistance;
                    previous[next] = current;
                    queue.Enqueue(next, candidateDistance);
                }
            }

            return null;
        }

        private float GetRiverPathCost(
            int seed,
            int source,
            WorldCell from,
            WorldCell to,
            bool hasExistingRiver,
            bool reversesExistingRiver)
        {
            if (IsWaterCell(to))
            {
                return 0.05f;
            }

            var heightDelta = to.Height - from.Height;
            var cost = 1f;
            if (heightDelta > 0f)
            {
                cost += heightDelta * riverSettings.UphillStepPenalty;
            }
            else
            {
                cost += Math.Abs(heightDelta) * riverSettings.DownhillStepPenalty;
            }

            var waterDistance = to.DistanceToWater == int.MaxValue ? 0 : to.DistanceToWater;
            cost += waterDistance * riverSettings.WaterDistanceWeight;
            cost -= to.Moisture * riverSettings.DownhillMoistureWeight;
            if (hasExistingRiver)
            {
                cost -= riverSettings.ExistingRiverMergeBonus;
            }

            if (reversesExistingRiver)
            {
                cost += riverSettings.UphillStepPenalty;
            }

            if (to.Biome == BiomeType.Mountains)
            {
                cost += riverSettings.MountainTracePenalty;
            }
            else if (to.Biome == BiomeType.Swamp || to.Biome == BiomeType.Forest)
            {
                cost -= riverSettings.WetBiomeTraceDiscount;
            }

            cost += SphericalWorldNoise.Noise(
                to.SpherePosition,
                seed,
                riverSettings.DownhillNoiseSaltBase + source,
                riverSettings.DownhillNoiseOctaves,
                riverSettings.DownhillNoiseFrequency,
                noiseSettings) * riverSettings.DownhillNoiseWeight;

            return Math.Max(0.05f, cost);
        }

        private static void ApplyRiverData(WorldCell[] cells, WorldRiverEdge[] riverEdges)
        {
            var bestDownstreamFlow = new float[cells.Length];
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                bestDownstreamFlow[cellIndex] = -1f;
                cells[cellIndex].HasRiver = false;
                cells[cellIndex].RiverFlow = 0f;
                cells[cellIndex].DownstreamCellId = WorldIds.None;
            }

            for (var edgeIndex = 0; edgeIndex < riverEdges.Length; edgeIndex++)
            {
                var edge = riverEdges[edgeIndex];
                cells[edge.FromCellId].HasRiver = true;
                cells[edge.ToCellId].HasRiver = true;
                cells[edge.FromCellId].RiverFlow += edge.Flow;
                cells[edge.ToCellId].RiverFlow += edge.Flow * 0.35f;
                if (edge.Flow > bestDownstreamFlow[edge.FromCellId])
                {
                    bestDownstreamFlow[edge.FromCellId] = edge.Flow;
                    cells[edge.FromCellId].DownstreamCellId = edge.ToCellId;
                }
            }
        }

        private static WorldRoadEdge[] ApplyRoads(WorldCell[] cells, Dictionary<ulong, RoadType> roadEdges)
        {
            var result = new List<WorldRoadEdge>(roadEdges.Count);
            foreach (var pair in roadEdges)
            {
                var from = (int)(pair.Key >> 32);
                var to = (int)(pair.Key & 0xffffffff);
                var roadType = pair.Value;
                ApplyRoadToCell(ref cells[from], roadType);
                ApplyRoadToCell(ref cells[to], roadType);
                result.Add(new WorldRoadEdge
                {
                    FromCellId = from,
                    ToCellId = to,
                    RoadType = roadType
                });
            }

            result.Sort((left, right) =>
            {
                var fromCompare = left.FromCellId.CompareTo(right.FromCellId);
                return fromCompare != 0 ? fromCompare : left.ToCellId.CompareTo(right.ToCellId);
            });
            return result.ToArray();
        }

        private static void ApplyRoadToCell(ref WorldCell cell, RoadType roadType)
        {
            if (GetRoadRank(roadType) > GetRoadRank(cell.RoadType))
            {
                cell.RoadType = roadType;
            }

            cell.HasRoad = cell.RoadType != RoadType.None;
        }

        private static void ApplyMovementCosts(WorldCell[] cells)
        {
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!cells[cellIndex].IsPassable)
                {
                    cells[cellIndex].MovementCost = WorldMovementCosts.ImpassableCost;
                    continue;
                }

                cells[cellIndex].MovementCost = WorldMovementCosts.Calculate(
                    cells[cellIndex].Biome,
                    cells[cellIndex].RoadType,
                cells[cellIndex].HasRiver);
            }
        }

        private static int[] CalculateDistanceToWater(WorldCell[] cells, List<int>[] neighbours)
        {
            var distances = new int[cells.Length];
            for (var cellIndex = 0; cellIndex < distances.Length; cellIndex++)
            {
                distances[cellIndex] = int.MaxValue;
            }

            var queue = new Queue<int>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!IsWaterCell(cells[cellIndex]))
                {
                    continue;
                }

                distances[cellIndex] = 0;
                queue.Enqueue(cellIndex);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var nextDistance = distances[current] + 1;
                for (var neighbourIndex = 0; neighbourIndex < neighbours[current].Count; neighbourIndex++)
                {
                    var next = neighbours[current][neighbourIndex];
                    if (distances[next] <= nextDistance)
                    {
                        continue;
                    }

                    distances[next] = nextDistance;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static void ApplyDistanceToWater(WorldCell[] cells, int[] distances)
        {
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                cells[cellIndex].DistanceToWater = distances[cellIndex];
            }
        }

        private void ApplyHydrologyBiomesAndResources(int seed, WorldCell[] cells)
        {
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var cell = cells[cellIndex];
                if (!IsWaterCell(cell))
                {
                    var moistureBonus = 0f;
                    if (cell.HasRiver)
                    {
                        moistureBonus += riverSettings.RiverValleyMoistureBonus;
                        moistureBonus += Math.Min(
                            riverSettings.RiverFlowMoistureBonusMax,
                            cell.RiverFlow * riverSettings.RiverFlowMoistureBonusPerFlow);
                    }

                    if (cell.DistanceToWater <= 2)
                    {
                        moistureBonus += riverSettings.NearWaterMoistureBonus;
                    }

                    if (moistureBonus > 0f)
                    {
                        cell.Moisture = Clamp01(cell.Moisture + moistureBonus);
                    }

                    cell.Biome = AdjustBiomeForHydrology(cell, seed, cellIndex);
                    cell.IsPassable = cell.Biome != BiomeType.Ocean &&
                                      !(cell.Biome == BiomeType.Mountains &&
                                        cell.Height >= terrainSettings.ImpassableMountainThreshold);
                }

                cell.ResourceAmount = SphericalTerrainGenerator.CalculateResourceAmount(
                    cell.Biome,
                    cell.Height,
                    cell.Moisture,
                    seed,
                    cellIndex,
                    cell.RiverFlow,
                    cell.DistanceToWater);
                cell.MovementCost = cell.IsPassable
                    ? WorldMovementCosts.Calculate(cell.Biome, cell.RoadType, cell.HasRiver)
                    : WorldMovementCosts.ImpassableCost;
                cells[cellIndex] = cell;
            }
        }

        private BiomeType AdjustBiomeForHydrology(WorldCell cell, int seed, int cellIndex)
        {
            if (cell.Biome == BiomeType.Mountains || cell.Biome == BiomeType.Snow)
            {
                return cell.Biome;
            }

            if (cell.HasRiver &&
                (cell.Biome == BiomeType.Desert ||
                 cell.Biome == BiomeType.RustDesert ||
                 cell.Biome == BiomeType.AshWastes ||
                 cell.Biome == BiomeType.DemonScar))
            {
                return cell.Moisture > terrainSettings.ForestMinMoisture
                    ? BiomeType.Forest
                    : BiomeType.Plains;
            }

            if (cell.Height < terrainSettings.SwampMaxHeight &&
                cell.Moisture > terrainSettings.SwampMinMoisture &&
                (cell.HasRiver || cell.DistanceToWater <= 2))
            {
                return cell.Temperature > terrainSettings.ToxicSwampMinTemperature &&
                       SphericalWorldNoise.Hash01(seed, cellIndex, 1741) > terrainSettings.ToxicSwampChanceThreshold
                    ? BiomeType.ToxicSwamp
                    : BiomeType.Swamp;
            }

            if (cell.Moisture > terrainSettings.ForestMinMoisture &&
                (cell.Biome == BiomeType.Plains || cell.Biome == BiomeType.Desert))
            {
                return cell.Temperature < terrainSettings.DeadForestMaxTemperature &&
                       SphericalWorldNoise.Hash01(seed, cellIndex, 1747) > terrainSettings.DeadForestChanceThreshold
                    ? BiomeType.DeadForest
                    : BiomeType.Forest;
            }

            return cell.Biome;
        }

        private static void AddRoadPath(
            IReadOnlyList<int> path,
            RoadType roadType,
            Dictionary<ulong, RoadType> roadEdges)
        {
            if (path == null || path.Count < 2)
            {
                return;
            }

            for (var pathIndex = 1; pathIndex < path.Count; pathIndex++)
            {
                var from = path[pathIndex - 1];
                var to = path[pathIndex];
                var key = GetUndirectedEdgeKey(from, to);
                if (!roadEdges.TryGetValue(key, out var existing) || GetRoadRank(roadType) > GetRoadRank(existing))
                {
                    roadEdges[key] = roadType;
                }
            }
        }

        private static HashSet<int> CollectRoadCells(Dictionary<ulong, RoadType> roadEdges, RoadType minimumRoadType)
        {
            var cells = new HashSet<int>();
            foreach (var pair in roadEdges)
            {
                if (GetRoadRank(pair.Value) < GetRoadRank(minimumRoadType))
                {
                    continue;
                }

                cells.Add((int)(pair.Key >> 32));
                cells.Add((int)(pair.Key & 0xffffffff));
            }

            return cells;
        }

        private static List<int> PickNetworkSatellites(
            int seed,
            WorldCell[] cells,
            List<int>[] neighbours,
            HashSet<int> network,
            int count,
            int minimumNetworkDistance,
            int maximumNetworkDistance,
            IReadOnlyList<SettlementData> settlements,
            WorldRoadGenerationSettings roadSettings,
            WorldNoiseSettings noiseSettings)
        {
            var result = new List<int>(count);
            if (network.Count == 0)
            {
                return result;
            }

            var distances = GetNetworkDistances(neighbours, network);
            var occupied = new HashSet<int>();
            for (var settlementIndex = 0; settlementIndex < settlements.Count; settlementIndex++)
            {
                occupied.Add(settlements[settlementIndex].CellId);
            }

            var candidates = new List<ScoredCell>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!cells[cellIndex].IsPassable || occupied.Contains(cellIndex))
                {
                    continue;
                }

                var distance = distances[cellIndex];
                if (distance >= minimumNetworkDistance && distance <= maximumNetworkDistance)
                {
                    candidates.Add(new ScoredCell(
                        cellIndex,
                        SatelliteScore(seed, cells[cellIndex], distance, roadSettings, noiseSettings)));
                }
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));

            for (var candidateIndex = 0; candidateIndex < candidates.Count && result.Count < count; candidateIndex++)
            {
                var candidate = candidates[candidateIndex].CellId;
                if (!IsFarFromSelection(
                        cells,
                        candidate,
                        result,
                        ScaleAngularDistance(cells.Length, roadSettings.SatelliteMinAngularDistance, roadSettings)))
                {
                    continue;
                }

                result.Add(candidate);
            }

            return result;
        }

        private readonly struct ScoredCell
        {
            public ScoredCell(int cellId, float score)
            {
                CellId = cellId;
                Score = score;
            }

            public int CellId { get; }

            public float Score { get; }
        }

        private static float SatelliteScore(
            int seed,
            WorldCell cell,
            int distance,
            WorldRoadGenerationSettings roadSettings,
            WorldNoiseSettings noiseSettings)
        {
            var terrainScore = !IsWaterCell(cell) && cell.Height < roadSettings.SatelliteMaxHeight
                ? roadSettings.SatellitePreferredTerrainScore
                : roadSettings.SatelliteFallbackTerrainScore;
            var distanceScore = roadSettings.SatelliteDistanceScoreWeight / Math.Max(1, distance);
            var waterScore = 0f;
            if (cell.HasRiver)
            {
                waterScore += roadSettings.SatelliteRiverScoreBonus;
            }

            if (cell.Biome == BiomeType.Coast)
            {
                waterScore += roadSettings.SatelliteCoastScoreBonus;
            }
            else if (cell.DistanceToWater <= 2)
            {
                waterScore += roadSettings.SatelliteNearWaterScoreBonus;
            }

            var noiseScore = SphericalWorldNoise.Noise(
                cell.SpherePosition,
                seed,
                roadSettings.SatelliteNoiseSalt,
                roadSettings.SatelliteNoiseOctaves,
                roadSettings.SatelliteNoiseFrequency,
                noiseSettings) * roadSettings.SatelliteNoiseWeight;
            return terrainScore + distanceScore + waterScore + noiseScore;
        }

        private static int ScaleGraphDistance(int cellCount, int baseDistance, WorldRoadGenerationSettings roadSettings)
        {
            var scale = Math.Sqrt(Math.Max(1, cellCount) / (double)roadSettings.GraphDistanceReferenceCellCount);
            return Math.Max(baseDistance, (int)Math.Round(baseDistance * scale));
        }

        private static float ScaleAngularDistance(int cellCount, float baseDistance, WorldRoadGenerationSettings roadSettings)
        {
            var scale = Math.Sqrt(roadSettings.AngularDistanceReferenceCellCount / (double)Math.Max(1, cellCount));
            return Math.Max(roadSettings.SatelliteMinimumScaledAngularDistance, (float)(baseDistance * scale));
        }

        private static int[] GetNetworkDistances(List<int>[] neighbours, HashSet<int> network)
        {
            var distances = new int[neighbours.Length];
            for (var index = 0; index < distances.Length; index++)
            {
                distances[index] = int.MaxValue;
            }

            var queue = new Queue<int>();
            foreach (var cellId in network)
            {
                distances[cellId] = 0;
                queue.Enqueue(cellId);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var nextDistance = distances[current] + 1;
                for (var neighbourIndex = 0; neighbourIndex < neighbours[current].Count; neighbourIndex++)
                {
                    var next = neighbours[current][neighbourIndex];
                    if (distances[next] <= nextDistance)
                    {
                        continue;
                    }

                    distances[next] = nextDistance;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static List<int> FindPathToNetwork(
            WorldCell[] cells,
            List<int>[] neighbours,
            int start,
            HashSet<int> network,
            WorldRoadGenerationSettings roadSettings)
        {
            if (network.Count == 0)
            {
                return null;
            }

            return FindPath(cells, neighbours, start, candidate => candidate != start && network.Contains(candidate), roadSettings);
        }

        private static List<int> FindPath(
            WorldCell[] cells,
            List<int>[] neighbours,
            int start,
            int target,
            WorldRoadGenerationSettings roadSettings)
        {
            return FindPath(cells, neighbours, start, candidate => candidate == target, roadSettings);
        }

        private static List<int> FindPath(
            WorldCell[] cells,
            List<int>[] neighbours,
            int start,
            Predicate<int> isTarget,
            WorldRoadGenerationSettings roadSettings)
        {
            var count = cells.Length;
            var distances = new float[count];
            var previous = new int[count];
            var visited = new bool[count];
            for (var cellIndex = 0; cellIndex < count; cellIndex++)
            {
                distances[cellIndex] = float.MaxValue;
                previous[cellIndex] = WorldIds.None;
            }

            distances[start] = 0f;
            var queue = new PathPriorityQueue(Math.Max(roadSettings.PathQueueMinCapacity, count / roadSettings.PathQueueCellDivisor));
            queue.Enqueue(start, 0f);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var current = node.CellId;
                if (visited[current])
                {
                    continue;
                }

                if (isTarget(current))
                {
                    return ReconstructPath(previous, current);
                }

                visited[current] = true;
                for (var neighbourIndex = 0; neighbourIndex < neighbours[current].Count; neighbourIndex++)
                {
                    var next = neighbours[current][neighbourIndex];
                    if (visited[next] || !cells[next].IsPassable)
                    {
                        continue;
                    }

                    var edgeCost = GetPathCost(cells[current], cells[next], roadSettings);
                    var candidateDistance = distances[current] + edgeCost;
                    if (candidateDistance < distances[next])
                    {
                        distances[next] = candidateDistance;
                        previous[next] = current;
                        queue.Enqueue(next, candidateDistance);
                    }
                }
            }

            return null;
        }

        private static float GetPathCost(WorldCell from, WorldCell to, WorldRoadGenerationSettings roadSettings)
        {
            var cost = (float)WorldMovementCosts.GetBiomeCost(to.Biome);
            if (to.HasRiver)
            {
                cost += roadSettings.PathRiverPenalty;
                cost -= roadSettings.PathRiverValleyDiscount;
            }

            if (from.Biome == BiomeType.Mountains || to.Biome == BiomeType.Mountains)
            {
                cost += roadSettings.PathMountainPenalty;
            }

            if (to.Biome == BiomeType.Coast || to.DistanceToWater <= 2)
            {
                cost -= roadSettings.PathCoastDiscount;
            }

            if (to.Height > from.Height)
            {
                cost += (to.Height - from.Height) * roadSettings.PathUphillPenalty;
            }

            return Math.Max(1f, cost);
        }

        private static List<int> ReconstructPath(int[] previous, int target)
        {
            var path = new List<int>();
            for (var current = target; current != WorldIds.None; current = previous[current])
            {
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private readonly struct PathQueueNode
        {
            public readonly int CellId;
            public readonly float Distance;

            public PathQueueNode(int cellId, float distance)
            {
                CellId = cellId;
                Distance = distance;
            }
        }

        private sealed class PathPriorityQueue
        {
            private readonly List<PathQueueNode> nodes;

            public PathPriorityQueue(int capacity)
            {
                nodes = new List<PathQueueNode>(capacity);
            }

            public int Count => nodes.Count;

            public void Enqueue(int cellId, float distance)
            {
                nodes.Add(new PathQueueNode(cellId, distance));
                SiftUp(nodes.Count - 1);
            }

            public PathQueueNode Dequeue()
            {
                var result = nodes[0];
                var lastIndex = nodes.Count - 1;
                nodes[0] = nodes[lastIndex];
                nodes.RemoveAt(lastIndex);
                if (nodes.Count > 0)
                {
                    SiftDown(0);
                }

                return result;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (nodes[parent].Distance <= nodes[index].Distance)
                    {
                        return;
                    }

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void SiftDown(int index)
            {
                while (true)
                {
                    var left = index * 2 + 1;
                    var right = left + 1;
                    var smallest = index;

                    if (left < nodes.Count && nodes[left].Distance < nodes[smallest].Distance)
                    {
                        smallest = left;
                    }

                    if (right < nodes.Count && nodes[right].Distance < nodes[smallest].Distance)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int left, int right)
            {
                var temp = nodes[left];
                nodes[left] = nodes[right];
                nodes[right] = temp;
            }
        }

        private static void CreateGeodesicGrid(
            int targetCellCount,
            out WorldSpherePoint[] positions,
            out List<int>[] neighbours)
        {
            var frequency = ResolveGeodesicFrequency(targetCellCount);
            var expectedCellCount = 10 * frequency * frequency + 2;
            var positionList = new List<WorldSpherePoint>(expectedCellCount);
            var vertexIds = new Dictionary<GeodesicVertexKey, int>(expectedCellCount);
            var triangles = new List<GeodesicTriangle>(IcosahedronFaceCount * frequency * frequency);
            var baseVertices = CreateIcosahedronVertices();
            var baseFaces = CreateIcosahedronFaces();

            for (var faceIndex = 0; faceIndex < IcosahedronFaceCount; faceIndex++)
            {
                var cornerA = baseFaces[faceIndex, 0];
                var cornerB = baseFaces[faceIndex, 1];
                var cornerC = baseFaces[faceIndex, 2];
                var lattice = new int[frequency + 1, frequency + 1];

                for (var bWeight = 0; bWeight <= frequency; bWeight++)
                {
                    var maxCWeight = frequency - bWeight;
                    for (var cWeight = 0; cWeight <= maxCWeight; cWeight++)
                    {
                        var aWeight = frequency - bWeight - cWeight;
                        lattice[bWeight, cWeight] = GetOrCreateGeodesicVertex(
                            faceIndex,
                            cornerA,
                            cornerB,
                            cornerC,
                            aWeight,
                            bWeight,
                            cWeight,
                            frequency,
                            baseVertices,
                            positionList,
                            vertexIds);
                    }
                }

                for (var bWeight = 0; bWeight < frequency; bWeight++)
                {
                    var maxCWeight = frequency - bWeight;
                    for (var cWeight = 0; cWeight < maxCWeight; cWeight++)
                    {
                        var v0 = lattice[bWeight, cWeight];
                        var v1 = lattice[bWeight + 1, cWeight];
                        var v2 = lattice[bWeight, cWeight + 1];
                        triangles.Add(new GeodesicTriangle(v0, v1, v2));

                        if (cWeight >= maxCWeight - 1)
                        {
                            continue;
                        }

                        var v3 = lattice[bWeight + 1, cWeight + 1];
                        triangles.Add(new GeodesicTriangle(v1, v3, v2));
                    }
                }
            }

            positions = positionList.ToArray();
            neighbours = new List<int>[positions.Length];
            for (var cellIndex = 0; cellIndex < neighbours.Length; cellIndex++)
            {
                neighbours[cellIndex] = new List<int>(NeighbourCount);
            }

            for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                var triangle = triangles[triangleIndex];
                AddTriangleNeighbours(neighbours, triangle.A, triangle.B, triangle.C);
            }

            SortGeodesicNeighbours(positions, neighbours);
        }

        private static int ResolveGeodesicFrequency(int targetCellCount)
        {
            if (targetCellCount <= IcosahedronVertexCount)
            {
                return 1;
            }

            var idealFrequency = Math.Sqrt(Math.Max(1.0, (targetCellCount - 2) / 10.0));
            return Math.Max(1, (int)Math.Ceiling(idealFrequency));
        }

        private static int GetOrCreateGeodesicVertex(
            int faceIndex,
            int cornerA,
            int cornerB,
            int cornerC,
            int aWeight,
            int bWeight,
            int cWeight,
            int frequency,
            WorldSpherePoint[] baseVertices,
            List<WorldSpherePoint> positions,
            Dictionary<GeodesicVertexKey, int> vertexIds)
        {
            var key = CreateGeodesicVertexKey(
                faceIndex,
                cornerA,
                cornerB,
                cornerC,
                aWeight,
                bWeight,
                cWeight,
                frequency);
            if (vertexIds.TryGetValue(key, out var existingId))
            {
                return existingId;
            }

            var point = InterpolateOnBaseFace(
                baseVertices[cornerA],
                baseVertices[cornerB],
                baseVertices[cornerC],
                aWeight,
                bWeight,
                cWeight,
                frequency);
            var cellId = positions.Count;
            positions.Add(point);
            vertexIds.Add(key, cellId);
            return cellId;
        }

        private static GeodesicVertexKey CreateGeodesicVertexKey(
            int faceIndex,
            int cornerA,
            int cornerB,
            int cornerC,
            int aWeight,
            int bWeight,
            int cWeight,
            int frequency)
        {
            if (aWeight == frequency)
            {
                return GeodesicVertexKey.Corner(cornerA);
            }

            if (bWeight == frequency)
            {
                return GeodesicVertexKey.Corner(cornerB);
            }

            if (cWeight == frequency)
            {
                return GeodesicVertexKey.Corner(cornerC);
            }

            if (cWeight == 0)
            {
                return GeodesicVertexKey.Edge(cornerA, cornerB, bWeight, frequency);
            }

            if (bWeight == 0)
            {
                return GeodesicVertexKey.Edge(cornerA, cornerC, cWeight, frequency);
            }

            if (aWeight == 0)
            {
                return GeodesicVertexKey.Edge(cornerB, cornerC, cWeight, frequency);
            }

            return GeodesicVertexKey.Face(faceIndex, bWeight, cWeight);
        }

        private static WorldSpherePoint InterpolateOnBaseFace(
            WorldSpherePoint a,
            WorldSpherePoint b,
            WorldSpherePoint c,
            int aWeight,
            int bWeight,
            int cWeight,
            int frequency)
        {
            var inverseFrequency = 1.0 / frequency;
            return SphericalWorldGeometry.Normalize(new WorldSpherePoint(
                (float)((a.X * aWeight + b.X * bWeight + c.X * cWeight) * inverseFrequency),
                (float)((a.Y * aWeight + b.Y * bWeight + c.Y * cWeight) * inverseFrequency),
                (float)((a.Z * aWeight + b.Z * bWeight + c.Z * cWeight) * inverseFrequency)));
        }

        private static WorldSpherePoint[] CreateIcosahedronVertices()
        {
            var phi = (1.0 + Math.Sqrt(5.0)) * 0.5;
            return new[]
            {
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(-1f, (float)phi, 0f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(1f, (float)phi, 0f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(-1f, (float)-phi, 0f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(1f, (float)-phi, 0f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(0f, -1f, (float)phi)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(0f, 1f, (float)phi)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(0f, -1f, (float)-phi)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint(0f, 1f, (float)-phi)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint((float)phi, 0f, -1f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint((float)phi, 0f, 1f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint((float)-phi, 0f, -1f)),
                SphericalWorldGeometry.Normalize(new WorldSpherePoint((float)-phi, 0f, 1f))
            };
        }

        private static int[,] CreateIcosahedronFaces()
        {
            return new[,]
            {
                { 0, 11, 5 },
                { 0, 5, 1 },
                { 0, 1, 7 },
                { 0, 7, 10 },
                { 0, 10, 11 },
                { 1, 5, 9 },
                { 5, 11, 4 },
                { 11, 10, 2 },
                { 10, 7, 6 },
                { 7, 1, 8 },
                { 3, 9, 4 },
                { 3, 4, 2 },
                { 3, 2, 6 },
                { 3, 6, 8 },
                { 3, 8, 9 },
                { 4, 9, 5 },
                { 2, 4, 11 },
                { 6, 2, 10 },
                { 8, 6, 7 },
                { 9, 8, 1 }
            };
        }

        private static void AddTriangleNeighbours(List<int>[] neighbours, int a, int b, int c)
        {
            AddGeodesicNeighbour(neighbours, a, b);
            AddGeodesicNeighbour(neighbours, b, c);
            AddGeodesicNeighbour(neighbours, c, a);
        }

        private static void AddGeodesicNeighbour(List<int>[] neighbours, int a, int b)
        {
            if (!neighbours[a].Contains(b))
            {
                neighbours[a].Add(b);
            }

            if (!neighbours[b].Contains(a))
            {
                neighbours[b].Add(a);
            }
        }

        private static void SortGeodesicNeighbours(WorldSpherePoint[] positions, List<int>[] neighbours)
        {
            for (var cellIndex = 0; cellIndex < neighbours.Length; cellIndex++)
            {
                var center = positions[cellIndex];
                var upHint = Math.Abs(center.Y) > 0.9f
                    ? new WorldSpherePoint(1f, 0f, 0f)
                    : new WorldSpherePoint(0f, 1f, 0f);
                var tangent = SphericalWorldGeometry.Normalize(Cross(upHint, center));
                var bitangent = SphericalWorldGeometry.Normalize(Cross(center, tangent));
                neighbours[cellIndex].Sort((left, right) =>
                    GetNeighbourAngle(positions[left], tangent, bitangent)
                        .CompareTo(GetNeighbourAngle(positions[right], tangent, bitangent)));
            }
        }

        private static double GetNeighbourAngle(WorldSpherePoint point, WorldSpherePoint tangent, WorldSpherePoint bitangent)
        {
            return Math.Atan2(
                SphericalWorldGeometry.Dot(point, bitangent),
                SphericalWorldGeometry.Dot(point, tangent));
        }

        private static WorldSpherePoint Cross(WorldSpherePoint left, WorldSpherePoint right)
        {
            return new WorldSpherePoint(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }

        private static WorldSpherePoint[] CreateFibonacciSphere(int count)
        {
            var points = new WorldSpherePoint[count];
            var goldenAngle = Math.PI * (3.0 - Math.Sqrt(5.0));
            for (var index = 0; index < count; index++)
            {
                var t = count == 1 ? 0.5 : (double)index / (count - 1);
                var y = 1.0 - t * 2.0;
                var radius = Math.Sqrt(Math.Max(0.0, 1.0 - y * y));
                var theta = goldenAngle * index;
                points[index] = new WorldSpherePoint(
                    (float)(Math.Cos(theta) * radius),
                    (float)y,
                    (float)(Math.Sin(theta) * radius));
            }

            return points;
        }

        private static List<int>[] BuildNeighbours(WorldSpherePoint[] positions)
        {
            var neighbours = new List<int>[positions.Length];
            var spatialIndex = new SphereSpatialIndex(positions);
            var candidateBuffer = new List<int>(NeighbourCount * NeighbourCandidateMultiplier);
            for (var index = 0; index < neighbours.Length; index++)
            {
                neighbours[index] = FindNearestNeighbours(positions, spatialIndex, candidateBuffer, index, NeighbourCount);
            }

            MakeNeighboursSymmetric(positions, neighbours);
            EnsureConnected(positions, neighbours);
            return neighbours;
        }

        private static List<int> FindNearestNeighbours(
            WorldSpherePoint[] positions,
            SphereSpatialIndex spatialIndex,
            List<int> candidateBuffer,
            int cellIndex,
            int count)
        {
            var nearest = new List<int>(count);
            var nearestDistances = new List<float>(count);
            candidateBuffer.Clear();
            spatialIndex.CollectCandidates(
                positions[cellIndex],
                candidateBuffer,
                Math.Max(count * NeighbourCandidateMultiplier, count + 1));

            for (var candidateIndex = 0; candidateIndex < candidateBuffer.Count; candidateIndex++)
            {
                var candidate = candidateBuffer[candidateIndex];
                if (candidate == cellIndex)
                {
                    continue;
                }

                InsertNearestNeighbour(positions, cellIndex, candidate, count, nearest, nearestDistances);
            }

            if (nearest.Count >= count)
            {
                return nearest;
            }

            for (var candidate = 0; candidate < positions.Length && nearest.Count < count; candidate++)
            {
                if (candidate != cellIndex)
                {
                    InsertNearestNeighbour(positions, cellIndex, candidate, count, nearest, nearestDistances);
                }
            }

            return nearest;
        }

        private static void InsertNearestNeighbour(
            WorldSpherePoint[] positions,
            int cellIndex,
            int candidate,
            int count,
            List<int> nearest,
            List<float> nearestDistances)
        {
            var distance = 1f - SphericalWorldGeometry.Dot(positions[cellIndex], positions[candidate]);
            var insertIndex = 0;
            while (insertIndex < nearestDistances.Count && distance >= nearestDistances[insertIndex])
            {
                insertIndex++;
            }

            if (insertIndex >= count || nearest.Contains(candidate))
            {
                return;
            }

            nearest.Insert(insertIndex, candidate);
            nearestDistances.Insert(insertIndex, distance);
            if (nearest.Count > count)
            {
                nearest.RemoveAt(nearest.Count - 1);
                nearestDistances.RemoveAt(nearestDistances.Count - 1);
            }
        }

        private static void MakeNeighboursSymmetric(WorldSpherePoint[] positions, List<int>[] neighbours)
        {
            for (var cellIndex = 0; cellIndex < neighbours.Length; cellIndex++)
            {
                for (var neighbourIndex = 0; neighbourIndex < neighbours[cellIndex].Count; neighbourIndex++)
                {
                    var neighbour = neighbours[cellIndex][neighbourIndex];
                    if (!neighbours[neighbour].Contains(cellIndex))
                    {
                        ReplaceFarthestNeighbour(positions, neighbours[neighbour], neighbour, cellIndex);
                    }
                }
            }
        }

        private static void EnsureConnected(WorldSpherePoint[] positions, List<int>[] neighbours)
        {
            while (true)
            {
                var componentIds = GetComponentIds(neighbours, out var componentCount);
                if (componentCount <= 1)
                {
                    return;
                }

                var representatives = GetComponentRepresentatives(componentIds, componentCount);
                var bestA = 0;
                var bestB = 0;
                var bestDistance = float.MaxValue;
                for (var fromComponent = 0; fromComponent < representatives.Length; fromComponent++)
                {
                    var a = representatives[fromComponent];
                    for (var toComponent = fromComponent + 1; toComponent < representatives.Length; toComponent++)
                    {
                        var b = representatives[toComponent];
                        var distance = 1f - SphericalWorldGeometry.Dot(positions[a], positions[b]);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }

                ReplaceFarthestNeighbour(positions, neighbours[bestA], bestA, bestB);
                ReplaceFarthestNeighbour(positions, neighbours[bestB], bestB, bestA);
            }
        }

        private static int[] GetComponentRepresentatives(int[] componentIds, int componentCount)
        {
            var representatives = new int[componentCount];
            for (var componentIndex = 0; componentIndex < representatives.Length; componentIndex++)
            {
                representatives[componentIndex] = WorldIds.None;
            }

            for (var cellIndex = 0; cellIndex < componentIds.Length; cellIndex++)
            {
                var componentId = componentIds[cellIndex];
                if (representatives[componentId] == WorldIds.None)
                {
                    representatives[componentId] = cellIndex;
                }
            }

            return representatives;
        }

        private static int[] GetComponentIds(List<int>[] neighbours, out int componentCount)
        {
            var componentIds = new int[neighbours.Length];
            for (var index = 0; index < componentIds.Length; index++)
            {
                componentIds[index] = WorldIds.None;
            }

            componentCount = 0;
            var queue = new Queue<int>();
            for (var start = 0; start < neighbours.Length; start++)
            {
                if (componentIds[start] != WorldIds.None)
                {
                    continue;
                }

                componentIds[start] = componentCount;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    for (var neighbourIndex = 0; neighbourIndex < neighbours[current].Count; neighbourIndex++)
                    {
                        var next = neighbours[current][neighbourIndex];
                        if (componentIds[next] == WorldIds.None)
                        {
                            componentIds[next] = componentCount;
                            queue.Enqueue(next);
                        }

                        if (neighbours[next].Contains(current))
                        {
                            continue;
                        }

                        if (componentIds[next] == WorldIds.None)
                        {
                            componentIds[next] = componentCount;
                            queue.Enqueue(next);
                        }
                    }
                }

                componentCount++;
            }

            return componentIds;
        }

        private static void ReplaceFarthestNeighbour(
            WorldSpherePoint[] positions,
            List<int> neighbours,
            int owner,
            int replacement)
        {
            if (neighbours.Contains(replacement))
            {
                return;
            }

            var farthestIndex = 0;
            var farthestDistance = float.MinValue;
            for (var neighbourIndex = 0; neighbourIndex < neighbours.Count; neighbourIndex++)
            {
                var distance = 1f - SphericalWorldGeometry.Dot(positions[owner], positions[neighbours[neighbourIndex]]);
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndex = neighbourIndex;
                }
            }

            neighbours[farthestIndex] = replacement;
        }

        private static CellNeighbours[] ToCellNeighbours(List<int>[] neighbours)
        {
            var result = new CellNeighbours[neighbours.Length];
            for (var cellIndex = 0; cellIndex < neighbours.Length; cellIndex++)
            {
                result[cellIndex] = new CellNeighbours
                {
                    N0 = GetNeighbour(neighbours[cellIndex], 0),
                    N1 = GetNeighbour(neighbours[cellIndex], 1),
                    N2 = GetNeighbour(neighbours[cellIndex], 2),
                    N3 = GetNeighbour(neighbours[cellIndex], 3),
                    N4 = GetNeighbour(neighbours[cellIndex], 4),
                    N5 = GetNeighbour(neighbours[cellIndex], 5)
                };
            }

            return result;
        }

        private static int GetNeighbour(IReadOnlyList<int> neighbours, int index)
        {
            return index < neighbours.Count ? neighbours[index] : WorldIds.None;
        }

        private sealed class SphereSpatialIndex
        {
            private readonly WorldSpherePoint[] positions;
            private readonly float bucketSize;
            private readonly Dictionary<long, List<int>> buckets;

            public SphereSpatialIndex(WorldSpherePoint[] positions)
            {
                this.positions = positions;
                var averageSpacing = Math.Sqrt(4.0 * Math.PI / Math.Max(1, positions.Length));
                bucketSize = (float)Math.Max(
                    MinimumNeighbourBucketSize,
                    Math.Min(MaximumNeighbourBucketSize, averageSpacing * NeighbourBucketSpacingMultiplier));
                buckets = new Dictionary<long, List<int>>(positions.Length);

                for (var index = 0; index < positions.Length; index++)
                {
                    GetBucketCoordinates(positions[index], out var x, out var y, out var z);
                    var key = PackBucketKey(x, y, z);
                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        bucket = new List<int>(8);
                        buckets.Add(key, bucket);
                    }

                    bucket.Add(index);
                }
            }

            public void CollectCandidates(
                WorldSpherePoint point,
                List<int> candidates,
                int targetCandidateCount)
            {
                GetBucketCoordinates(point, out var centerX, out var centerY, out var centerZ);
                var maxRing = Math.Max(2, (int)Math.Ceiling(2f / bucketSize));
                for (var ring = 0; ring <= maxRing; ring++)
                {
                    for (var x = centerX - ring; x <= centerX + ring; x++)
                    {
                        for (var y = centerY - ring; y <= centerY + ring; y++)
                        {
                            for (var z = centerZ - ring; z <= centerZ + ring; z++)
                            {
                                if (ring > 0 &&
                                    Math.Abs(x - centerX) < ring &&
                                    Math.Abs(y - centerY) < ring &&
                                    Math.Abs(z - centerZ) < ring)
                                {
                                    continue;
                                }

                                if (buckets.TryGetValue(PackBucketKey(x, y, z), out var bucket))
                                {
                                    candidates.AddRange(bucket);
                                }
                            }
                        }
                    }

                    if (candidates.Count >= targetCandidateCount)
                    {
                        return;
                    }
                }
            }

            private void GetBucketCoordinates(WorldSpherePoint point, out int x, out int y, out int z)
            {
                x = FastFloor((point.X + 1f) / bucketSize);
                y = FastFloor((point.Y + 1f) / bucketSize);
                z = FastFloor((point.Z + 1f) / bucketSize);
            }

            private static int FastFloor(float value)
            {
                var integer = (int)value;
                return value < integer ? integer - 1 : integer;
            }

            private static long PackBucketKey(int x, int y, int z)
            {
                unchecked
                {
                    var hash = 1469598103934665603L;
                    hash = (hash ^ x) * 1099511628211L;
                    hash = (hash ^ y) * 1099511628211L;
                    hash = (hash ^ z) * 1099511628211L;
                    return hash;
                }
            }
        }

        private static bool IsFarFromSelection(WorldCell[] cells, int candidate, IReadOnlyList<int> selected, float minimumDistance)
        {
            for (var selectedIndex = 0; selectedIndex < selected.Count; selectedIndex++)
            {
                if (1f - SphericalWorldGeometry.Dot(cells[candidate].SpherePosition, cells[selected[selectedIndex]].SpherePosition) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFarFromSelection(WorldCell[] cells, int candidate, IReadOnlyList<SettlementData> selected, float minimumDistance)
        {
            for (var selectedIndex = 0; selectedIndex < selected.Count; selectedIndex++)
            {
                if (1f - SphericalWorldGeometry.Dot(cells[candidate].SpherePosition, cells[selected[selectedIndex].CellId].SpherePosition) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetStartingDay()
        {
            return configSnapshot.StartingDay;
        }

        private float GetStartingDominantInfluence()
        {
            return Math.Max(
                GlobalGenerationConfig.MinimumStartingDominantInfluence,
                configSnapshot.StartingDominantInfluence);
        }

        private int GetPlayerStartingCredits()
        {
            return configSnapshot.PlayerStartingCredits;
        }

        private int GetPlayerStartCell(WorldCell[] cells, FactionData[] factions)
        {
            var configuredStart = configSnapshot.PlayerStartCellId;
            if (IsValidCell(configuredStart, cells.Length) && cells[configuredStart].IsPassable)
            {
                return configuredStart;
            }

            return factions.Length > 0 ? factions[0].CapitalCellId : 0;
        }

        private static bool IsRiverSourceCandidate(WorldCell cell, WorldRiverGenerationSettings riverSettings)
        {
            return cell.IsPassable &&
                   !IsWaterCell(cell) &&
                   cell.Height >= riverSettings.SourceMinHeight &&
                   cell.Moisture >= riverSettings.SourceMinMoisture;
        }

        private static bool IsRiverTraceable(WorldCell cell)
        {
            return IsWaterCell(cell) || cell.IsPassable;
        }

        private static bool IsWaterCell(WorldCell cell)
        {
            return cell.Biome == BiomeType.Ocean || cell.Biome == BiomeType.Coast;
        }

        private static bool IsValidCell(int cellId, int cellCount)
        {
            return cellId >= 0 && cellId < cellCount;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }

        private static int GetRoadRank(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Small:
                    return 1;
                case RoadType.Medium:
                    return 2;
                case RoadType.Large:
                    return 3;
                default:
                    return 0;
            }
        }

        private static ulong GetUndirectedEdgeKey(int from, int to)
        {
            var min = (uint)Math.Min(from, to);
            var max = (uint)Math.Max(from, to);
            return ((ulong)min << 32) | max;
        }

        private static ulong GetDirectedEdgeKey(int from, int to)
        {
            return ((ulong)(uint)from << 32) | (uint)to;
        }

        private readonly struct RiverBuildResult
        {
            public readonly WorldRiverData[] Rivers;
            public readonly WorldRiverEdge[] Edges;

            public RiverBuildResult(WorldRiverData[] rivers, WorldRiverEdge[] edges)
            {
                Rivers = rivers ?? Array.Empty<WorldRiverData>();
                Edges = edges ?? Array.Empty<WorldRiverEdge>();
            }
        }

        private readonly struct GeodesicTriangle
        {
            public readonly int A;
            public readonly int B;
            public readonly int C;

            public GeodesicTriangle(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        private readonly struct GeodesicVertexKey : IEquatable<GeodesicVertexKey>
        {
            private readonly int type;
            private readonly int a;
            private readonly int b;
            private readonly int c;

            private GeodesicVertexKey(int type, int a, int b, int c)
            {
                this.type = type;
                this.a = a;
                this.b = b;
                this.c = c;
            }

            public static GeodesicVertexKey Corner(int corner)
            {
                return new GeodesicVertexKey(0, corner, 0, 0);
            }

            public static GeodesicVertexKey Edge(int start, int end, int stepFromStart, int frequency)
            {
                var min = Math.Min(start, end);
                var max = Math.Max(start, end);
                var stepFromMin = start == min ? stepFromStart : frequency - stepFromStart;
                return new GeodesicVertexKey(1, min, max, stepFromMin);
            }

            public static GeodesicVertexKey Face(int faceIndex, int bWeight, int cWeight)
            {
                return new GeodesicVertexKey(2, faceIndex, bWeight, cWeight);
            }

            public bool Equals(GeodesicVertexKey other)
            {
                return type == other.type &&
                       a == other.a &&
                       b == other.b &&
                       c == other.c;
            }

            public override bool Equals(object obj)
            {
                return obj is GeodesicVertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = type;
                    hash = hash * 397 ^ a;
                    hash = hash * 397 ^ b;
                    hash = hash * 397 ^ c;
                    return hash;
                }
            }
        }

    }
}
