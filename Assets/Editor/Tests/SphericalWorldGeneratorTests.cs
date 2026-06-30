using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using MercLord.Global.Generation;
using NUnit.Framework;

namespace MercLord.Editor.Tests
{
    public sealed class SphericalWorldGeneratorTests
    {
        private const int Seed = 424242;
        private const int CellCount = 1200;

        [Test]
        public void SameSeedProducesSameWorld()
        {
            var left = Generate(Seed);
            var right = Generate(Seed);

            Assert.AreEqual(left.Cells.Length, right.Cells.Length);
            Assert.AreEqual(left.RoadEdges.Length, right.RoadEdges.Length);
            Assert.AreEqual(left.Rivers.Length, right.Rivers.Length);
            Assert.AreEqual(left.RiverEdges.Length, right.RiverEdges.Length);
            for (var cellIndex = 0; cellIndex < left.Cells.Length; cellIndex++)
            {
                Assert.AreEqual(left.Cells[cellIndex].Biome, right.Cells[cellIndex].Biome);
                Assert.AreEqual(left.Cells[cellIndex].RoadType, right.Cells[cellIndex].RoadType);
                Assert.AreEqual(left.Cells[cellIndex].MovementCost, right.Cells[cellIndex].MovementCost);
                Assert.AreEqual(left.Cells[cellIndex].DominantFactionId, right.Cells[cellIndex].DominantFactionId);
                Assert.AreEqual(left.Cells[cellIndex].ResourceAmount, right.Cells[cellIndex].ResourceAmount);
                Assert.AreEqual(left.Cells[cellIndex].RiverFlow, right.Cells[cellIndex].RiverFlow);
                Assert.AreEqual(left.Cells[cellIndex].DownstreamCellId, right.Cells[cellIndex].DownstreamCellId);
                Assert.AreEqual(left.Cells[cellIndex].DistanceToWater, right.Cells[cellIndex].DistanceToWater);
            }

            for (var riverIndex = 0; riverIndex < left.Rivers.Length; riverIndex++)
            {
                Assert.AreEqual(left.Rivers[riverIndex].SourceCellId, right.Rivers[riverIndex].SourceCellId);
                Assert.AreEqual(left.Rivers[riverIndex].MouthCellId, right.Rivers[riverIndex].MouthCellId);
                Assert.AreEqual(left.Rivers[riverIndex].EdgeCount, right.Rivers[riverIndex].EdgeCount);
                Assert.AreEqual(left.Rivers[riverIndex].TotalFlow, right.Rivers[riverIndex].TotalFlow);
            }

            for (var edgeIndex = 0; edgeIndex < left.RiverEdges.Length; edgeIndex++)
            {
                Assert.AreEqual(left.RiverEdges[edgeIndex].RiverId, right.RiverEdges[edgeIndex].RiverId);
                Assert.AreEqual(left.RiverEdges[edgeIndex].FromCellId, right.RiverEdges[edgeIndex].FromCellId);
                Assert.AreEqual(left.RiverEdges[edgeIndex].ToCellId, right.RiverEdges[edgeIndex].ToCellId);
                Assert.AreEqual(left.RiverEdges[edgeIndex].Flow, right.RiverEdges[edgeIndex].Flow);
            }
        }

        [Test]
        public async System.Threading.Tasks.Task GenerateAsyncProducesWorld()
        {
            var generator = new SphericalWorldGenerator();
            var world = await generator.GenerateAsync(new WorldGenerationRequest(Seed, CellCount));

            Assert.AreEqual(ExpectedGeodesicCellCount(CellCount), world.Cells.Length);
            Assert.Greater(world.RoadEdges.Length, 0);
            Assert.Greater(world.RiverEdges.Length, 0);
        }

        [Test]
        public void InfluenceCountMatchesFactionCount()
        {
            var world = Generate(Seed);

            Assert.Greater(world.Factions.Length, 0);
            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                Assert.AreEqual(
                    world.Factions.Length,
                    world.Cells[cellIndex].Influence.Count,
                    $"Cell {cellIndex} influence count must equal configured faction count.");
            }
        }

        [Test]
        public void FactionControlIsEvenlyPartitioned()
        {
            var world = Generate(Seed);
            var counts = new Dictionary<int, int>();
            for (var factionIndex = 0; factionIndex < world.Factions.Length; factionIndex++)
            {
                counts[world.Factions[factionIndex].Id] = 0;
            }

            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                counts[world.Cells[cellIndex].OwnerFactionId]++;
            }

            var expected = world.Cells.Length / (float)world.Factions.Length;
            foreach (var pair in counts)
            {
                Assert.LessOrEqual(
                    Math.Abs(pair.Value - expected),
                    expected * 0.15f,
                    $"Faction {pair.Key} should own a balanced region of the globe.");
            }
        }

        [Test]
        public void FactionControlBuildsContiguousRegions()
        {
            var world = Generate(Seed);
            for (var factionIndex = 0; factionIndex < world.Factions.Length; factionIndex++)
            {
                var faction = world.Factions[factionIndex];
                var ownedCount = CountOwnedCells(world, faction.Id);
                var reachableCount = CountReachableFactionCells(world, faction);

                Assert.Greater(ownedCount, 0);
                Assert.AreEqual(
                    ownedCount,
                    reachableCount,
                    $"Faction {faction.Id} should own one contiguous region around its capital.");
            }
        }

        [Test]
        public void MapMarkersInheritTheirCellFaction()
        {
            var world = Generate(Seed);
            for (var settlementIndex = 0; settlementIndex < world.Settlements.Length; settlementIndex++)
            {
                var settlement = world.Settlements[settlementIndex];
                Assert.AreEqual(
                    world.Cells[settlement.CellId].OwnerFactionId,
                    settlement.FactionId,
                    $"Settlement {settlement.Id} should use the faction that owns its cell.");
            }

            for (var activityIndex = 0; activityIndex < world.Activities.Length; activityIndex++)
            {
                var activity = world.Activities[activityIndex];
                Assert.AreEqual(
                    world.Cells[activity.CellId].OwnerFactionId,
                    activity.FactionId,
                    $"Activity {activity.Id} should use the faction that owns its cell.");
            }
        }

        [Test]
        public void CellsStoreMapInfoForTooltip()
        {
            var world = Generate(Seed);
            var factionIds = new HashSet<int>();
            var cellsWithResources = 0;
            for (var factionIndex = 0; factionIndex < world.Factions.Length; factionIndex++)
            {
                factionIds.Add(world.Factions[factionIndex].Id);
            }

            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                var cell = world.Cells[cellIndex];
                Assert.AreEqual(cellIndex, cell.Id);
                Assert.IsTrue(Enum.IsDefined(typeof(BiomeType), cell.Biome));
                Assert.IsTrue(factionIds.Contains(cell.OwnerFactionId), $"Cell {cell.Id} must have a generated owning faction.");
                Assert.GreaterOrEqual(cell.ResourceAmount, 0);

                if (cell.ResourceAmount > 0)
                {
                    cellsWithResources++;
                }
            }

            Assert.Greater(cellsWithResources, 0);
        }

        [Test]
        public void CellGraphIsConnected()
        {
            var world = Generate(Seed);

            Assert.AreEqual(world.Cells.Length, CountReachableCells(world));
        }

        [Test]
        public void WaterCoverageFollowsConfiguredTarget()
        {
            var world = Generate(Seed);
            var oceanCount = 0;
            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                if (world.Cells[cellIndex].Biome == BiomeType.Ocean)
                {
                    oceanCount++;
                }
            }

            var coverage = oceanCount / (float)world.Cells.Length;
            Assert.LessOrEqual(
                Math.Abs(coverage - WorldTerrainGenerationSettings.Default.TargetWaterCoverage),
                0.02f,
                "Ocean coverage should follow configured percentage water coverage.");
        }

        [Test]
        public void GeodesicGridUsesTwelvePentagonsAndHexagons()
        {
            var world = Generate(Seed);
            var pentagons = 0;
            var hexagons = 0;

            for (var cellIndex = 0; cellIndex < world.Neighbours.Length; cellIndex++)
            {
                var neighbourCount = CountValidNeighbours(world.Neighbours[cellIndex]);
                if (neighbourCount == 5)
                {
                    pentagons++;
                    continue;
                }

                if (neighbourCount == 6)
                {
                    hexagons++;
                    continue;
                }

                Assert.Fail($"Cell {cellIndex} must be a pentagon or hexagon, but has {neighbourCount} neighbours.");
            }

            Assert.AreEqual(12, pentagons);
            Assert.AreEqual(world.Cells.Length - 12, hexagons);
        }

        [Test]
        public void RoadGraphIsConnected()
        {
            var world = Generate(Seed);
            var roadCells = CollectRoadCells(world, RoadType.Small);

            Assert.Greater(roadCells.Count, 0);
            Assert.AreEqual(roadCells.Count, CountReachableRoadCells(world, roadCells));
        }

        [Test]
        public void RoadTiersAttachToTheirParents()
        {
            var world = Generate(Seed);
            var largeCells = CollectRoadCells(world, RoadType.Large);
            var mediumCells = CollectRoadCells(world, RoadType.Medium);
            var smallCells = CollectRoadCells(world, RoadType.Small);

            Assert.Greater(largeCells.Count, 0);
            Assert.IsTrue(HasAdjacentRoadTier(world, mediumCells, largeCells), "At least one medium road must attach to the large road network.");
            Assert.IsTrue(HasAdjacentRoadTier(world, smallCells, mediumCells) || HasAdjacentRoadTier(world, smallCells, largeCells),
                "At least one small road must attach to a medium or large road.");
        }

        [Test]
        public void RiversFlowDownhillOrToWater()
        {
            var world = Generate(Seed);

            Assert.Greater(world.Rivers.Length, 0);
            Assert.Greater(world.RiverEdges.Length, 0);
            for (var edgeIndex = 0; edgeIndex < world.RiverEdges.Length; edgeIndex++)
            {
                var edge = world.RiverEdges[edgeIndex];
                var from = world.Cells[edge.FromCellId];
                var to = world.Cells[edge.ToCellId];
                var reachesWater = IsWaterCell(to);
                Assert.Greater(edge.Flow, 0f, $"River edge {edgeIndex} must have positive flow.");
                Assert.IsTrue(
                    to.Height <= from.Height + 0.12f ||
                    to.DistanceToWater <= from.DistanceToWater ||
                    reachesWater,
                    $"River edge {edgeIndex} should move downhill, toward water, or end at water.");
            }
        }

        [Test]
        public void RiversHaveHighlandSourcesAndWaterMouths()
        {
            var world = Generate(Seed);

            Assert.Greater(world.Rivers.Length, 0);
            for (var riverIndex = 0; riverIndex < world.Rivers.Length; riverIndex++)
            {
                var river = world.Rivers[riverIndex];
                Assert.AreEqual(riverIndex, river.Id);
                Assert.IsTrue(IsValidCell(river.SourceCellId, world.Cells.Length));
                Assert.IsTrue(IsValidCell(river.MouthCellId, world.Cells.Length));
                Assert.Greater(river.EdgeCount, 0);
                Assert.Greater(river.TotalFlow, 0f);

                var source = world.Cells[river.SourceCellId];
                var mouth = world.Cells[river.MouthCellId];
                Assert.IsTrue(source.IsPassable, $"River {river.Id} source must be passable.");
                Assert.IsFalse(IsWaterCell(source), $"River {river.Id} source must start on land.");
                Assert.GreaterOrEqual(
                    source.Height,
                    WorldRiverGenerationSettings.Default.SourceMinHeight - 0.0001f,
                    $"River {river.Id} source must start in highlands.");
                Assert.IsTrue(IsWaterCell(mouth), $"River {river.Id} mouth must end in ocean or coast.");
            }
        }

        [Test]
        public void SettlementsAndActivitiesPreferWaterOrRoads()
        {
            var world = Generate(Seed);
            var nearUsefulFeature = 0;
            var markerCount = world.Settlements.Length + world.Activities.Length;

            for (var settlementIndex = 0; settlementIndex < world.Settlements.Length; settlementIndex++)
            {
                if (IsNearWaterRiverOrRoad(world, world.Settlements[settlementIndex].CellId))
                {
                    nearUsefulFeature++;
                }
            }

            for (var activityIndex = 0; activityIndex < world.Activities.Length; activityIndex++)
            {
                if (IsNearWaterRiverOrRoad(world, world.Activities[activityIndex].CellId))
                {
                    nearUsefulFeature++;
                }
            }

            Assert.Greater(markerCount, 0);
            Assert.GreaterOrEqual(
                nearUsefulFeature,
                markerCount * 2 / 3,
                "Most settlements and activities should appear near roads, rivers, or coastlines.");
        }

        [Test]
        public void MovementCostsRespectRoadAndTerrainOrdering()
        {
            Assert.Less(
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.Large, false),
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.Medium, false));
            Assert.Less(
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.Medium, false),
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.Small, false));
            Assert.Less(
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.Small, false),
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.None, false));
            Assert.Greater(
                WorldMovementCosts.Calculate(BiomeType.Mountains, RoadType.None, false),
                WorldMovementCosts.Calculate(BiomeType.Forest, RoadType.None, false));
            Assert.Greater(
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.None, true),
                WorldMovementCosts.Calculate(BiomeType.Plains, RoadType.None, false));
            Assert.AreEqual(
                WorldMovementCosts.ImpassableCost,
                WorldMovementCosts.Calculate(BiomeType.Ocean, RoadType.None, false));
        }

        [Test]
        public void HighestMountainsAreImpassable()
        {
            var world = Generate(Seed);
            var impassableMountainCount = 0;
            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                var cell = world.Cells[cellIndex];
                if (cell.Biome != BiomeType.Mountains || cell.IsPassable)
                {
                    continue;
                }

                impassableMountainCount++;
                Assert.AreEqual(WorldMovementCosts.ImpassableCost, cell.MovementCost);
            }

            Assert.Greater(impassableMountainCount, 0);
        }

        private static WorldModel Generate(int seed)
        {
            var generator = new SphericalWorldGenerator();
            return generator.Generate(new WorldGenerationRequest(seed, CellCount));
        }

        private static int ExpectedGeodesicCellCount(int targetCellCount)
        {
            var frequency = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(Math.Max(1.0, (targetCellCount - 2) / 10.0))));
            return 10 * frequency * frequency + 2;
        }

        private static int CountReachableCells(WorldModel world)
        {
            var visited = new bool[world.Cells.Length];
            var queue = new Queue<int>();
            visited[0] = true;
            queue.Enqueue(0);
            var count = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                count++;
                VisitNeighbour(GetNeighbours(world.Neighbours[current]), visited, queue);
            }

            return count;
        }

        private static int CountReachableRoadCells(WorldModel world, HashSet<int> roadCells)
        {
            var roadGraph = BuildRoadGraph(world);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (var cellId in roadCells)
            {
                visited.Add(cellId);
                queue.Enqueue(cellId);
                break;
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!roadGraph.TryGetValue(current, out var neighbours))
                {
                    continue;
                }

                for (var index = 0; index < neighbours.Count; index++)
                {
                    var next = neighbours[index];
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count;
        }

        private static int CountOwnedCells(WorldModel world, int factionId)
        {
            var count = 0;
            for (var cellIndex = 0; cellIndex < world.Cells.Length; cellIndex++)
            {
                if (world.Cells[cellIndex].OwnerFactionId == factionId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountReachableFactionCells(WorldModel world, FactionData faction)
        {
            var startCellId = faction.CapitalCellId;
            Assert.AreEqual(faction.Id, world.Cells[startCellId].OwnerFactionId);

            var visited = new bool[world.Cells.Length];
            var queue = new Queue<int>();
            visited[startCellId] = true;
            queue.Enqueue(startCellId);
            var count = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                count++;
                var neighbours = GetNeighbours(world.Neighbours[current]);
                for (var neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                {
                    var next = neighbours[neighbourIndex];
                    if (next == WorldIds.None ||
                        visited[next] ||
                        world.Cells[next].OwnerFactionId != faction.Id)
                    {
                        continue;
                    }

                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }

            return count;
        }

        private static Dictionary<int, List<int>> BuildRoadGraph(WorldModel world)
        {
            var graph = new Dictionary<int, List<int>>();
            for (var edgeIndex = 0; edgeIndex < world.RoadEdges.Length; edgeIndex++)
            {
                var edge = world.RoadEdges[edgeIndex];
                AddRoadNeighbour(graph, edge.FromCellId, edge.ToCellId);
                AddRoadNeighbour(graph, edge.ToCellId, edge.FromCellId);
            }

            return graph;
        }

        private static void AddRoadNeighbour(Dictionary<int, List<int>> graph, int from, int to)
        {
            if (!graph.TryGetValue(from, out var neighbours))
            {
                neighbours = new List<int>();
                graph.Add(from, neighbours);
            }

            neighbours.Add(to);
        }

        private static HashSet<int> CollectRoadCells(WorldModel world, RoadType minimumRoadType)
        {
            var cells = new HashSet<int>();
            for (var edgeIndex = 0; edgeIndex < world.RoadEdges.Length; edgeIndex++)
            {
                var edge = world.RoadEdges[edgeIndex];
                if (RoadRank(edge.RoadType) < RoadRank(minimumRoadType))
                {
                    continue;
                }

                cells.Add(edge.FromCellId);
                cells.Add(edge.ToCellId);
            }

            return cells;
        }

        private static bool HasAdjacentRoadTier(WorldModel world, HashSet<int> children, HashSet<int> parents)
        {
            foreach (var child in children)
            {
                var neighbours = GetNeighbours(world.Neighbours[child]);
                for (var neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                {
                    if (parents.Contains(neighbours[neighbourIndex]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void VisitNeighbour(int[] neighbours, bool[] visited, Queue<int> queue)
        {
            for (var neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
            {
                var next = neighbours[neighbourIndex];
                if (next == WorldIds.None || visited[next])
                {
                    continue;
                }

                visited[next] = true;
                queue.Enqueue(next);
            }
        }

        private static int[] GetNeighbours(CellNeighbours neighbours)
        {
            return new[]
            {
                neighbours.N0,
                neighbours.N1,
                neighbours.N2,
                neighbours.N3,
                neighbours.N4,
                neighbours.N5
            };
        }

        private static int CountValidNeighbours(CellNeighbours neighbours)
        {
            var count = 0;
            var neighbourIds = GetNeighbours(neighbours);
            for (var neighbourIndex = 0; neighbourIndex < neighbourIds.Length; neighbourIndex++)
            {
                if (neighbourIds[neighbourIndex] != WorldIds.None)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsNearWaterRiverOrRoad(WorldModel world, int cellId)
        {
            if (!IsValidCell(cellId, world.Cells.Length))
            {
                return false;
            }

            var cell = world.Cells[cellId];
            if (cell.HasRoad || cell.HasRiver || cell.DistanceToWater <= 2)
            {
                return true;
            }

            var neighbours = GetNeighbours(world.Neighbours[cellId]);
            for (var neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
            {
                var neighbour = neighbours[neighbourIndex];
                if (!IsValidCell(neighbour, world.Cells.Length))
                {
                    continue;
                }

                var neighbourCell = world.Cells[neighbour];
                if (neighbourCell.HasRoad || neighbourCell.HasRiver || neighbourCell.DistanceToWater <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWaterCell(WorldCell cell)
        {
            return cell.Biome == BiomeType.Ocean || cell.Biome == BiomeType.Coast;
        }

        private static bool IsValidCell(int cellId, int cellCount)
        {
            return cellId >= 0 && cellId < cellCount;
        }

        private static int RoadRank(RoadType roadType)
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
    }
}
