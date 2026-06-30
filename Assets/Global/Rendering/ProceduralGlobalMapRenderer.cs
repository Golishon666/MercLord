using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using UnityEngine;
using UnityEngine.Rendering;

namespace MercLord.Global.Rendering
{
    public sealed class ProceduralGlobalMapRenderer : MonoBehaviour
    {
        private const int LineSegments = 8;
        private const int NeighbourCount = 6;
        private const int StarCount = 720;
        private const string GeneratedRootName = "Generated Map";
        private const string SelectionObjectName = "Selected Cell Highlight";
        private const HideFlags RuntimeGeneratedHideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        private const float HexAreaFactor = 2.598076f;
        private const float SelectionSurfaceOffset = 0.032f;
        private static readonly Color RiverColor = new(0.08f, 0.22f, 0.38f, 0.9f);
        private static readonly Color LargeRoadColor = new(0.16f, 0.15f, 0.13f, 0.95f);
        private static readonly Color MediumRoadColor = new(0.23f, 0.20f, 0.16f, 0.88f);
        private static readonly Color SmallRoadColor = new(0.18f, 0.17f, 0.15f, 0.74f);
        private static readonly Color SelectionColor = new(0.96f, 0.98f, 0.68f, 1f);

        private static readonly Vector2[] HouseShape =
        {
            new(-0.44f, -0.48f),
            new(0.44f, -0.48f),
            new(0.44f, 0.08f),
            new(0.00f, 0.56f),
            new(-0.44f, 0.08f)
        };

        private static readonly Vector2[] TriangleShape =
        {
            new(-0.48f, -0.42f),
            new(0.48f, -0.42f),
            new(0.00f, 0.54f)
        };

        private static readonly Vector2[] DiamondShape =
        {
            new(0.00f, -0.52f),
            new(0.48f, 0.00f),
            new(0.00f, 0.52f),
            new(-0.48f, 0.00f)
        };

        private static readonly Color[] FactionMarkerColors =
        {
            new(0.12f, 0.50f, 0.95f, 1f),
            new(0.45f, 0.34f, 0.86f, 1f),
            new(0.66f, 0.84f, 1.00f, 1f),
            new(0.30f, 0.78f, 0.44f, 1f),
            new(0.95f, 0.38f, 0.35f, 1f),
            new(0.95f, 0.70f, 0.78f, 1f)
        };

        private Transform generatedRoot;
        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private GlobalMapArtAtlas artAtlas;
        [SerializeField] private WorldModel currentWorld;
        [SerializeField] private float planetRadius = 3f;
        [SerializeField] private float starfieldRadius = 18f;
        [SerializeField] private float terrainSurfaceOffset = 0.006f;
        [SerializeField] private float biomeUnderlayOffset = -0.045f;
        [SerializeField, Range(12, 32)] private int tileVoronoiSeedVertexCount = 12;
        [SerializeField, Min(1f)] private float tileVoronoiSeedRadiusMultiplier = 2.4f;

        private Material vertexColorMaterial;
        private Material iconMaterial;
        private GameObject selectionObject;
        private int selectedCellId = WorldIds.None;

        public WorldModel CurrentWorld => currentWorld;
        public GlobalMapArtAtlas ArtAtlas => artAtlas;
        public int SelectedCellId => selectedCellId;
        public float PlanetRadius => planetRadius;
        private WorldTerrainGenerationSettings TerrainSettings => configDatabase?.GlobalGeneration?.Terrain ?? WorldTerrainGenerationSettings.Default;
        private WorldNoiseSettings NoiseSettings => configDatabase?.GlobalGeneration?.Noise ?? WorldNoiseSettings.Default;

        public void Configure(ConfigDatabase database)
        {
            configDatabase = database;
        }

        public void ConfigureArtAtlas(GlobalMapArtAtlas atlas)
        {
            artAtlas = atlas;
        }

        public void Render(WorldModel worldModel)
        {
            if (worldModel == null)
            {
                throw new ArgumentNullException(nameof(worldModel));
            }

            currentWorld = worldModel;
            EnsureRoot();
            ClearGenerated();
            EnsureMaterials();
            RenderStarfield(worldModel.Seed);
            RenderBiomeUnderlay(worldModel.Seed);
            RenderTerrain(worldModel);
            RenderRivers(worldModel);
            RenderRoads(worldModel);
            RenderMarkers(worldModel);
        }

        public void ClearGenerated()
        {
            EnsureRoot();
            for (var childIndex = generatedRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                DestroyGeneratedObject(generatedRoot.GetChild(childIndex).gameObject);
            }

            DestroyObject(vertexColorMaterial);
            vertexColorMaterial = null;
            DestroyObject(iconMaterial);
            iconMaterial = null;
            selectionObject = null;
            selectedCellId = WorldIds.None;
        }

        public bool TryGetCell(int cellId, out WorldCell cell)
        {
            var cells = currentWorld?.Cells ?? Array.Empty<WorldCell>();
            if (IsValidCell(cellId, cells.Length))
            {
                cell = cells[cellId];
                return true;
            }

            cell = default;
            return false;
        }

        public bool TryPickCell(Ray worldRay, out int cellId)
        {
            cellId = WorldIds.None;
            var cells = currentWorld?.Cells ?? Array.Empty<WorldCell>();
            if (cells.Length == 0 || !TryRaycastPlanet(worldRay, out var hitNormal))
            {
                return false;
            }

            var bestDot = float.MinValue;
            for (var index = 0; index < cells.Length; index++)
            {
                var dot = Vector3.Dot(hitNormal, ToVector(cells[index].SpherePosition).normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    cellId = index;
                }
            }

            return cellId != WorldIds.None;
        }

        public void SelectCell(int cellId)
        {
            var cells = currentWorld?.Cells ?? Array.Empty<WorldCell>();
            if (!IsValidCell(cellId, cells.Length))
            {
                ClearSelection();
                return;
            }

            selectedCellId = cellId;
            RenderSelection();
        }

        public void ClearSelection()
        {
            selectedCellId = WorldIds.None;
            DestroySelectionObject();
        }

        private void RenderStarfield(int seed)
        {
            var random = new System.Random(seed ^ 0x45C1A3D);
            var vertices = new List<Vector3>(StarCount * 4);
            var colors = new List<Color>(StarCount * 4);
            var triangles = new List<int>(StarCount * 6);

            for (var starIndex = 0; starIndex < StarCount; starIndex++)
            {
                var normal = RandomUnitVector(random);
                var tangent = Vector3.Cross(Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up, normal).normalized;
                var bitangent = Vector3.Cross(normal, tangent).normalized;
                var center = normal * starfieldRadius;
                var size = Mathf.Lerp(0.011f, 0.035f, (float)random.NextDouble());
                var brightness = Mathf.Lerp(0.55f, 1f, (float)random.NextDouble());
                var tint = Color.Lerp(new Color(0.68f, 0.78f, 1f, 1f), Color.white, (float)random.NextDouble());
                var color = tint * brightness;
                color.a = 1f;
                var vertexStart = vertices.Count;

                vertices.Add(center - tangent * size - bitangent * size);
                vertices.Add(center + tangent * size - bitangent * size);
                vertices.Add(center + tangent * size + bitangent * size);
                vertices.Add(center - tangent * size + bitangent * size);

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);

                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 1);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart + 3);
            }

            var mesh = CreateMesh("GlobalMap Starfield", vertices, colors, triangles);
            var starObject = CreateGeneratedGameObject("Starfield Mesh");
            starObject.transform.SetParent(generatedRoot, false);
            var meshFilter = starObject.AddComponent<MeshFilter>();
            var meshRenderer = starObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = vertexColorMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void RenderBiomeUnderlay(int seed)
        {
            const int lonSegments = 192;
            const int latSegments = 96;
            var vertices = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var normals = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var colors = new List<Color>((latSegments + 1) * (lonSegments + 1));
            var triangles = new List<int>(latSegments * lonSegments * 6);
            var radius = planetRadius + biomeUnderlayOffset;

            for (var lat = 0; lat <= latSegments; lat++)
            {
                var v = lat / (float)latSegments;
                var polar = v * Mathf.PI;
                var y = Mathf.Cos(polar);
                var ringRadius = Mathf.Sin(polar);
                for (var lon = 0; lon <= lonSegments; lon++)
                {
                    var u = lon / (float)lonSegments;
                    var azimuth = u * Mathf.PI * 2f;
                    var normal = new Vector3(Mathf.Cos(azimuth) * ringRadius, y, Mathf.Sin(azimuth) * ringRadius).normalized;
                    vertices.Add(normal * radius);
                    normals.Add(normal);
                    colors.Add(SampleVisualSurface(normal, seed).Color);
                }
            }

            var stride = lonSegments + 1;
            for (var lat = 0; lat < latSegments; lat++)
            {
                for (var lon = 0; lon < lonSegments; lon++)
                {
                    var a = lat * stride + lon;
                    var b = a + stride;
                    var c = b + 1;
                    var d = a + 1;

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(d);
                    triangles.Add(b);
                    triangles.Add(c);
                }
            }

            var mesh = CreateMesh("GlobalMap Biome Underlay", vertices, colors, triangles);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            var underlayObject = CreateGeneratedGameObject("Biome Underlay Mesh");
            underlayObject.transform.SetParent(generatedRoot, false);
            var meshFilter = underlayObject.AddComponent<MeshFilter>();
            var meshRenderer = underlayObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = vertexColorMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void RenderTerrain(WorldModel worldModel)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            if (cells.Length == 0)
            {
                return;
            }

            const int expectedVerticesPerTile = 7;
            var vertices = new List<Vector3>(cells.Length * expectedVerticesPerTile);
            var normals = new List<Vector3>(cells.Length * expectedVerticesPerTile);
            var colors = new List<Color>(cells.Length * expectedVerticesPerTile);
            var triangles = new List<int>(cells.Length * NeighbourCount * 3);
            var directNeighbours = new List<int>(NeighbourCount);
            var cornerNormals = new List<Vector3>(NeighbourCount);
            var terrainRadius = planetRadius + terrainSurfaceOffset;
            var neighbours = worldModel.Neighbours ?? Array.Empty<CellNeighbours>();

            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var cell = cells[cellIndex];
                var normal = ToVector(cell.SpherePosition).normalized;
                BuildGeodesicTileCornerNormals(cells, neighbours, cellIndex, directNeighbours, cornerNormals);
                if (cornerNormals.Count < 3)
                {
                    continue;
                }

                var centerIndex = vertices.Count;
                var centerColor = GetTileCenterColor(cell, worldModel.Seed);

                vertices.Add(normal * terrainRadius);
                normals.Add(normal);
                colors.Add(centerColor);

                for (var vertexIndex = 0; vertexIndex < cornerNormals.Count; vertexIndex++)
                {
                    var ringNormal = cornerNormals[vertexIndex];
                    vertices.Add(ringNormal * terrainRadius);
                    normals.Add(ringNormal);
                    colors.Add(GetTileEdgeColor(cell, worldModel.Seed, vertexIndex, centerColor));
                }

                for (var vertexIndex = 0; vertexIndex < cornerNormals.Count; vertexIndex++)
                {
                    triangles.Add(centerIndex);
                    triangles.Add(centerIndex + 1 + vertexIndex);
                    triangles.Add(centerIndex + 1 + ((vertexIndex + 1) % cornerNormals.Count));
                }
            }

            var mesh = CreateMesh("GlobalMap Terrain", vertices, colors, triangles);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            var terrainObject = CreateGeneratedGameObject("Terrain Mesh");
            terrainObject.transform.SetParent(generatedRoot, false);
            var meshFilter = terrainObject.AddComponent<MeshFilter>();
            var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = vertexColorMaterial;
        }

        private void RenderRivers(WorldModel worldModel)
        {
            var riverEdges = worldModel.RiverEdges ?? Array.Empty<WorldRiverEdge>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var vertices = new List<Vector3>(riverEdges.Length * LineSegments * 4);
            var normals = new List<Vector3>(riverEdges.Length * LineSegments * 4);
            var colors = new List<Color>(riverEdges.Length * LineSegments * 4);
            var triangles = new List<int>(riverEdges.Length * LineSegments * 6);

            for (var edgeIndex = 0; edgeIndex < riverEdges.Length; edgeIndex++)
            {
                var edge = riverEdges[edgeIndex];
                if (!IsValidEdge(edge.FromCellId, edge.ToCellId, cells.Length))
                {
                    continue;
                }

                var flowWidth = Mathf.Sqrt(Mathf.Max(0f, edge.Flow));
                var width = 0.0045f + Mathf.Clamp(flowWidth, 0f, 6f) * 0.0018f;
                AddSphericalRibbon(
                    vertices,
                    normals,
                    colors,
                    triangles,
                    cells[edge.FromCellId],
                    cells[edge.ToCellId],
                    width,
                    0.014f,
                    RiverColor);
            }

            CreateGeneratedMeshObject("Rivers Mesh", "GlobalMap Rivers", vertices, normals, colors, triangles, vertexColorMaterial);
        }

        private void RenderRoads(WorldModel worldModel)
        {
            var roadEdges = worldModel.RoadEdges ?? Array.Empty<WorldRoadEdge>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var vertices = new List<Vector3>(roadEdges.Length * LineSegments * 4);
            var normals = new List<Vector3>(roadEdges.Length * LineSegments * 4);
            var colors = new List<Color>(roadEdges.Length * LineSegments * 4);
            var triangles = new List<int>(roadEdges.Length * LineSegments * 6);

            for (var edgeIndex = 0; edgeIndex < roadEdges.Length; edgeIndex++)
            {
                var edge = roadEdges[edgeIndex];
                if (!IsValidEdge(edge.FromCellId, edge.ToCellId, cells.Length))
                {
                    continue;
                }

                var width = GetRoadWidth(edge.RoadType);
                AddSphericalRibbon(
                    vertices,
                    normals,
                    colors,
                    triangles,
                    cells[edge.FromCellId],
                    cells[edge.ToCellId],
                    width,
                    0.018f,
                    GetRoadColor(edge.RoadType));
            }

            CreateGeneratedMeshObject("Roads Mesh", "GlobalMap Roads", vertices, normals, colors, triangles, vertexColorMaterial);
        }

        private void RenderMarkers(WorldModel worldModel)
        {
            if (RenderTexturedMarkers(worldModel))
            {
                return;
            }

            var settlements = worldModel.Settlements ?? Array.Empty<SettlementData>();
            var activities = worldModel.Activities ?? Array.Empty<WorldActivityData>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var factions = worldModel.Factions ?? Array.Empty<FactionData>();
            var markerVertexCapacity = settlements.Length * (HouseShape.Length + 1) * 2 +
                                       activities.Length * (TriangleShape.Length + 1) * 2;
            var vertices = new List<Vector3>(markerVertexCapacity);
            var normals = new List<Vector3>(markerVertexCapacity);
            var colors = new List<Color>(markerVertexCapacity);
            var triangles = new List<int>((settlements.Length + activities.Length) * 24);

            for (var settlementIndex = 0; settlementIndex < settlements.Length; settlementIndex++)
            {
                var settlement = settlements[settlementIndex];
                if (!IsValidCell(settlement.CellId, cells.Length))
                {
                    continue;
                }

                var color = GetFactionMarkerColor(settlement.FactionId);
                var isCapital = IsCapitalCell(settlement.CellId, factions);
                var size = isCapital ? 0.17f : 0.115f;
                AddFlatIcon(vertices, normals, colors, triangles, cells[settlement.CellId], HouseShape, size, color, 0.034f);
            }

            for (var activityIndex = 0; activityIndex < activities.Length; activityIndex++)
            {
                var activity = activities[activityIndex];
                if (!IsValidCell(activity.CellId, cells.Length))
                {
                    continue;
                }

                var shape = activity.Type == WorldActivityType.CaravanStop ? DiamondShape : TriangleShape;
                var factionId = IsKnownFactionId(activity.FactionId, factions)
                    ? activity.FactionId
                    : cells[activity.CellId].OwnerFactionId;
                var color = GetFactionMarkerColor(factionId);
                AddFlatIcon(vertices, normals, colors, triangles, cells[activity.CellId], shape, 0.12f, color, 0.032f);
            }

            CreateGeneratedMeshObject("Markers Mesh", "GlobalMap Markers", vertices, normals, colors, triangles, vertexColorMaterial);
        }

        private bool RenderTexturedMarkers(WorldModel worldModel)
        {
            if (artAtlas == null ||
                artAtlas.IconAtlasTexture == null ||
                artAtlas.IconSprites == null ||
                artAtlas.IconSprites.Length == 0)
            {
                return false;
            }

            EnsureMaterials();
            if (iconMaterial == null)
            {
                return false;
            }

            var settlements = worldModel.Settlements ?? Array.Empty<SettlementData>();
            var activities = worldModel.Activities ?? Array.Empty<WorldActivityData>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var factions = worldModel.Factions ?? Array.Empty<FactionData>();
            var markerCount = settlements.Length + activities.Length;
            var vertices = new List<Vector3>(markerCount * 4);
            var normals = new List<Vector3>(markerCount * 4);
            var colors = new List<Color>(markerCount * 4);
            var uvs = new List<Vector2>(markerCount * 4);
            var triangles = new List<int>(markerCount * 6);

            for (var settlementIndex = 0; settlementIndex < settlements.Length; settlementIndex++)
            {
                var settlement = settlements[settlementIndex];
                if (!IsValidCell(settlement.CellId, cells.Length))
                {
                    continue;
                }

                var iconId = IsCapitalCell(settlement.CellId, factions)
                    ? GlobalMapIconSpriteId.Capital
                    : GlobalMapIconSpriteId.Town;
                if (!artAtlas.TryGetIconSprite(iconId, out var sprite))
                {
                    continue;
                }

                var size = iconId == GlobalMapIconSpriteId.Capital ? 0.17f : 0.125f;
                AddSpriteMarker(vertices, normals, colors, uvs, triangles, cells[settlement.CellId], sprite, size, Color.white, 0.04f);
            }

            for (var activityIndex = 0; activityIndex < activities.Length; activityIndex++)
            {
                var activity = activities[activityIndex];
                if (!IsValidCell(activity.CellId, cells.Length))
                {
                    continue;
                }

                if (!artAtlas.TryGetIconSprite(GetActivityIconId(activity.Type), out var sprite))
                {
                    continue;
                }

                AddSpriteMarker(vertices, normals, colors, uvs, triangles, cells[activity.CellId], sprite, 0.13f, Color.white, 0.038f);
            }

            CreateGeneratedTexturedMeshObject("Marker Icons Mesh", "GlobalMap Marker Icons", vertices, normals, colors, uvs, triangles, iconMaterial);
            return vertices.Count > 0;
        }

        private void RenderSelection()
        {
            var cells = currentWorld?.Cells ?? Array.Empty<WorldCell>();
            var neighbours = currentWorld?.Neighbours ?? Array.Empty<CellNeighbours>();
            if (!IsValidCell(selectedCellId, cells.Length))
            {
                ClearSelection();
                return;
            }

            EnsureRoot();
            EnsureMaterials();
            DestroySelectionObject();

            var cell = cells[selectedCellId];
            var normal = ToVector(cell.SpherePosition).normalized;
            var directNeighbours = new List<int>(NeighbourCount);
            var cornerNormals = new List<Vector3>(NeighbourCount);
            BuildGeodesicTileCornerNormals(cells, neighbours, selectedCellId, directNeighbours, cornerNormals);
            if (cornerNormals.Count < 3)
            {
                return;
            }

            var terrainRadius = planetRadius + SelectionSurfaceOffset;
            var vertices = new List<Vector3>(cornerNormals.Count + 1);
            var normals = new List<Vector3>(cornerNormals.Count + 1);
            var colors = new List<Color>(cornerNormals.Count + 1);
            var triangles = new List<int>(cornerNormals.Count * 3);
            var centerIndex = vertices.Count;
            vertices.Add(normal * terrainRadius);
            normals.Add(normal);
            colors.Add(SelectionColor);

            for (var vertexIndex = 0; vertexIndex < cornerNormals.Count; vertexIndex++)
            {
                var ringNormal = cornerNormals[vertexIndex];
                vertices.Add(ringNormal * terrainRadius);
                normals.Add(ringNormal);
                colors.Add(SelectionColor);
            }

            for (var vertexIndex = 0; vertexIndex < cornerNormals.Count; vertexIndex++)
            {
                triangles.Add(centerIndex);
                triangles.Add(centerIndex + 1 + vertexIndex);
                triangles.Add(centerIndex + 1 + ((vertexIndex + 1) % cornerNormals.Count));
            }

            var mesh = CreateMesh("GlobalMap Selected Cell", vertices, colors, triangles);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            selectionObject = CreateGeneratedGameObject(SelectionObjectName);
            selectionObject.transform.SetParent(generatedRoot, false);
            var meshFilter = selectionObject.AddComponent<MeshFilter>();
            var meshRenderer = selectionObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = vertexColorMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private bool TryRaycastPlanet(Ray worldRay, out Vector3 hitNormal)
        {
            var localOrigin = transform.InverseTransformPoint(worldRay.origin);
            var localDirection = transform.InverseTransformDirection(worldRay.direction).normalized;
            var radius = planetRadius + SelectionSurfaceOffset;
            var b = Vector3.Dot(localOrigin, localDirection);
            var c = localOrigin.sqrMagnitude - radius * radius;
            var discriminant = b * b - c;
            if (discriminant < 0f)
            {
                hitNormal = default;
                return false;
            }

            var root = Mathf.Sqrt(discriminant);
            var distance = -b - root;
            if (distance < 0f)
            {
                distance = -b + root;
            }

            if (distance < 0f)
            {
                hitNormal = default;
                return false;
            }

            hitNormal = (localOrigin + localDirection * distance).normalized;
            return true;
        }

        private void AddSphericalRibbon(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<int> triangles,
            WorldCell from,
            WorldCell to,
            float width,
            float offset,
            Color color)
        {
            var fromNormal = ToVector(from.SpherePosition).normalized;
            var toNormal = ToVector(to.SpherePosition).normalized;
            var radius = planetRadius + offset;
            var halfWidth = width * 0.5f;

            for (var segment = 0; segment < LineSegments; segment++)
            {
                var t = segment / (float)LineSegments;
                var nextT = (segment + 1) / (float)LineSegments;
                var startNormal = SlerpNormal(fromNormal, toNormal, t);
                var endNormal = SlerpNormal(fromNormal, toNormal, nextT);
                var start = startNormal * radius;
                var end = endNormal * radius;
                var tangent = (end - start).normalized;
                var startSide = Vector3.Cross(startNormal, tangent).normalized * halfWidth;
                var endSide = Vector3.Cross(endNormal, tangent).normalized * halfWidth;
                var vertexStart = vertices.Count;

                vertices.Add(start - startSide);
                vertices.Add(start + startSide);
                vertices.Add(end - endSide);
                vertices.Add(end + endSide);

                normals.Add(startNormal);
                normals.Add(startNormal);
                normals.Add(endNormal);
                normals.Add(endNormal);

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);

                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 1);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart + 1);
                triangles.Add(vertexStart + 3);
            }
        }

        private void AddFlatIcon(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<int> triangles,
            WorldCell cell,
            IReadOnlyList<Vector2> shape,
            float size,
            Color color,
            float offset)
        {
            var normal = ToVector(cell.SpherePosition).normalized;
            var position = GetCellPosition(cell, offset);
            GetMarkerBasis(normal, out var tangentRight, out var tangentUp);

            AddFlatIconPart(vertices, normals, colors, triangles, shape, size * 1.14f, Color.black, position, normal, tangentRight, tangentUp);
            AddFlatIconPart(vertices, normals, colors, triangles, shape, size, color, normal * 0.003f + position, normal, tangentRight, tangentUp);
        }

        private static void AddFlatIconPart(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<int> triangles,
            IReadOnlyList<Vector2> shape,
            float size,
            Color color,
            Vector3 localPosition,
            Vector3 normal,
            Vector3 tangentRight,
            Vector3 tangentUp)
        {
            var vertexStart = vertices.Count;
            vertices.Add(localPosition);
            normals.Add(normal);
            colors.Add(color);

            for (var pointIndex = 0; pointIndex < shape.Count; pointIndex++)
            {
                var point = shape[pointIndex] * size;
                vertices.Add(localPosition + tangentRight * point.x + tangentUp * point.y);
                normals.Add(normal);
                colors.Add(color);
            }

            for (var pointIndex = 0; pointIndex < shape.Count; pointIndex++)
            {
                triangles.Add(vertexStart);
                triangles.Add(vertexStart + pointIndex + 1);
                triangles.Add(vertexStart + 1 + ((pointIndex + 1) % shape.Count));
            }
        }

        private void AddSpriteMarker(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            WorldCell cell,
            Sprite sprite,
            float size,
            Color color,
            float offset)
        {
            var texture = sprite.texture;
            if (texture == null)
            {
                return;
            }

            var normal = ToVector(cell.SpherePosition).normalized;
            var position = GetCellPosition(cell, offset);
            GetMarkerBasis(normal, out var tangentRight, out var tangentUp);

            var rect = sprite.textureRect;
            var aspect = rect.width / Mathf.Max(1f, rect.height);
            var halfHeight = size;
            var halfWidth = size * aspect;
            var vertexStart = vertices.Count;
            var nudgedPosition = position + normal * 0.004f;

            vertices.Add(nudgedPosition - tangentRight * halfWidth - tangentUp * halfHeight);
            vertices.Add(nudgedPosition + tangentRight * halfWidth - tangentUp * halfHeight);
            vertices.Add(nudgedPosition + tangentRight * halfWidth + tangentUp * halfHeight);
            vertices.Add(nudgedPosition - tangentRight * halfWidth + tangentUp * halfHeight);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            var uMin = rect.xMin / texture.width;
            var uMax = rect.xMax / texture.width;
            var vMin = rect.yMin / texture.height;
            var vMax = rect.yMax / texture.height;
            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMax, vMin));
            uvs.Add(new Vector2(uMax, vMax));
            uvs.Add(new Vector2(uMin, vMax));

            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
        }

        private Vector3 GetCellPosition(WorldCell cell, float offset)
        {
            return ToVector(cell.SpherePosition).normalized * GetSurfaceRadius(cell, offset);
        }

        private float GetSurfaceRadius(WorldCell cell, float offset)
        {
            return planetRadius + offset;
        }

        private float GetSurfaceRadius(SurfaceSample sample, float offset)
        {
            return planetRadius + offset;
        }

        private static void BuildGeodesicTileCornerNormals(
            WorldCell[] cells,
            CellNeighbours[] neighbours,
            int cellIndex,
            List<int> directNeighbours,
            List<Vector3> cornerNormals)
        {
            directNeighbours.Clear();
            cornerNormals.Clear();
            if (!IsValidCell(cellIndex, cells.Length) || cellIndex >= neighbours.Length)
            {
                return;
            }

            for (var neighbourSlot = 0; neighbourSlot < NeighbourCount; neighbourSlot++)
            {
                var neighbourId = GetNeighbourId(neighbours[cellIndex], neighbourSlot);
                if (IsValidCell(neighbourId, cells.Length) && !directNeighbours.Contains(neighbourId))
                {
                    directNeighbours.Add(neighbourId);
                }
            }

            if (directNeighbours.Count < 3)
            {
                return;
            }

            var centerNormal = ToVector(cells[cellIndex].SpherePosition).normalized;
            for (var neighbourIndex = 0; neighbourIndex < directNeighbours.Count; neighbourIndex++)
            {
                var leftNormal = ToVector(cells[directNeighbours[neighbourIndex]].SpherePosition).normalized;
                var rightNormal = ToVector(cells[directNeighbours[(neighbourIndex + 1) % directNeighbours.Count]].SpherePosition).normalized;
                cornerNormals.Add((centerNormal + leftNormal + rightNormal).normalized);
            }

            var winding = Vector3.Dot(
                Vector3.Cross(cornerNormals[0] - centerNormal, cornerNormals[1] - centerNormal),
                centerNormal);
            if (winding < 0f)
            {
                cornerNormals.Reverse();
            }
        }

        private void BuildTilePolygon(
            WorldCell[] cells,
            CellNeighbours[] neighbours,
            int cellIndex,
            Vector3 tangent,
            Vector3 bitangent,
            float fallbackRadius,
            List<int> clipNeighbours,
            List<Vector2> polygon,
            List<Vector2> clippedPolygon)
        {
            polygon.Clear();
            clippedPolygon.Clear();
            CollectClipNeighbourIds(neighbours, cellIndex, cells.Length, clipNeighbours);
            var seedRadius = fallbackRadius;
            var centerNormal = ToVector(cells[cellIndex].SpherePosition).normalized;

            for (var neighbourIndex = 0; neighbourIndex < clipNeighbours.Count; neighbourIndex++)
            {
                var neighbourNormal = ToVector(cells[clipNeighbours[neighbourIndex]].SpherePosition).normalized;
                var projected = new Vector2(
                    Vector3.Dot(neighbourNormal, tangent),
                    Vector3.Dot(neighbourNormal, bitangent));
                seedRadius = Mathf.Max(seedRadius, projected.magnitude);
            }

            CreateSeedPolygon(Mathf.Max(fallbackRadius, seedRadius * tileVoronoiSeedRadiusMultiplier), polygon);
            for (var neighbourIndex = 0; neighbourIndex < clipNeighbours.Count && polygon.Count >= 3; neighbourIndex++)
            {
                var neighbourNormal = ToVector(cells[clipNeighbours[neighbourIndex]].SpherePosition).normalized;
                if (Vector3.Dot(centerNormal, neighbourNormal) < 0f)
                {
                    continue;
                }

                var neighbourPoint = new Vector2(
                    Vector3.Dot(neighbourNormal, tangent),
                    Vector3.Dot(neighbourNormal, bitangent));
                var halfPlaneLength = neighbourPoint.sqrMagnitude;
                if (halfPlaneLength <= 0.000001f)
                {
                    continue;
                }

                ClipPolygonByHalfPlane(polygon, clippedPolygon, neighbourPoint, halfPlaneLength * 0.5f);
            }
        }

        private void CreateSeedPolygon(float radius, List<Vector2> polygon)
        {
            var vertexCount = Mathf.Max(12, tileVoronoiSeedVertexCount);
            polygon.Clear();
            for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                var angle = Mathf.PI * 2f * vertexIndex / vertexCount;
                polygon.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static void CollectClipNeighbourIds(
            CellNeighbours[] neighbours,
            int cellIndex,
            int cellCount,
            List<int> result)
        {
            result.Clear();
            AddUniqueNeighbours(result, neighbours, cellIndex, cellCount);
            var directCount = result.Count;
            for (var index = 0; index < directCount; index++)
            {
                AddUniqueNeighbours(result, neighbours, result[index], cellCount);
            }
        }

        private static void AddUniqueNeighbours(List<int> result, CellNeighbours[] neighbours, int cellIndex, int cellCount)
        {
            if (cellIndex < 0 || cellIndex >= neighbours.Length)
            {
                return;
            }

            for (var neighbourSlot = 0; neighbourSlot < 6; neighbourSlot++)
            {
                var neighbourId = GetNeighbourId(neighbours[cellIndex], neighbourSlot);
                if (!IsValidCell(neighbourId, cellCount) || result.Contains(neighbourId))
                {
                    continue;
                }

                result.Add(neighbourId);
            }
        }

        private static void ClipPolygonByHalfPlane(
            List<Vector2> polygon,
            List<Vector2> clipped,
            Vector2 normal,
            float maxDot)
        {
            if (polygon.Count == 0)
            {
                return;
            }

            clipped.Clear();
            var previous = polygon[polygon.Count - 1];
            var previousInside = Vector2.Dot(previous, normal) <= maxDot;

            for (var vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
            {
                var current = polygon[vertexIndex];
                var currentInside = Vector2.Dot(current, normal) <= maxDot;
                if (currentInside)
                {
                    if (!previousInside)
                    {
                        clipped.Add(IntersectHalfPlane(previous, current, normal, maxDot));
                    }

                    clipped.Add(current);
                }
                else if (previousInside)
                {
                    clipped.Add(IntersectHalfPlane(previous, current, normal, maxDot));
                }

                previous = current;
                previousInside = currentInside;
            }

            polygon.Clear();
            polygon.AddRange(clipped);
        }

        private static Vector2 IntersectHalfPlane(Vector2 from, Vector2 to, Vector2 normal, float maxDot)
        {
            var direction = to - from;
            var denominator = Vector2.Dot(direction, normal);
            if (Mathf.Abs(denominator) <= 0.000001f)
            {
                return to;
            }

            var t = (maxDot - Vector2.Dot(from, normal)) / denominator;
            return from + direction * Mathf.Clamp01(t);
        }

        private float GetFallbackTileAngularRadius(int cellCount)
        {
            var sphericalAreaPerCell = 4f * Mathf.PI / Mathf.Max(1, cellCount);
            var hexCircumradiusAngle = Mathf.Sqrt(sphericalAreaPerCell / HexAreaFactor);
            return Mathf.Tan(hexCircumradiusAngle);
        }

        private static int GetNeighbourId(CellNeighbours neighbours, int slot)
        {
            switch (slot)
            {
                case 0:
                    return neighbours.N0;
                case 1:
                    return neighbours.N1;
                case 2:
                    return neighbours.N2;
                case 3:
                    return neighbours.N3;
                case 4:
                    return neighbours.N4;
                case 5:
                    return neighbours.N5;
                default:
                    return WorldIds.None;
            }
        }

        private static Color GetTileCenterColor(WorldCell cell, int seed)
        {
            var normal = ToVector(cell.SpherePosition).normalized;
            var color = GetBiomeColor(cell.Biome);
            var broadNoise = Noise3(normal, seed, 9f, cell.Id * 0.071f + 3.1f);
            var fineNoise = Noise3(normal, seed, 32f, cell.Id * 0.037f + 17.3f);
            var heightShade = cell.Biome == BiomeType.Ocean ? 1f : Mathf.Lerp(0.88f, 1.12f, cell.Height);
            var textureShade = Mathf.Lerp(0.84f, 1.12f, broadNoise * 0.65f + fineNoise * 0.35f);

            if (cell.Biome == BiomeType.Forest || cell.Biome == BiomeType.DeadForest)
            {
                color = Color.Lerp(color, new Color(0.14f, 0.25f, 0.09f, 1f), fineNoise * 0.45f);
            }
            else if (cell.Biome == BiomeType.Mountains)
            {
                color = Color.Lerp(color, new Color(0.70f, 0.68f, 0.62f, 1f), Mathf.Clamp01((cell.Height - 0.62f) * 2.5f));
            }
            else if (cell.Biome == BiomeType.Ocean)
            {
                color = Color.Lerp(new Color(0.07f, 0.16f, 0.28f, 1f), color, Mathf.Lerp(0.65f, 1f, fineNoise));
                textureShade = Mathf.Lerp(0.94f, 1.06f, fineNoise);
            }

            return ShadeColor(color, heightShade * textureShade);
        }

        private static Color GetTileEdgeColor(WorldCell cell, int seed, int side, Color centerColor)
        {
            var normal = ToVector(cell.SpherePosition).normalized;
            var sideNoise = Noise3(normal, seed + side * 101, 48f, cell.Id * 0.053f + side * 11.7f);
            var edgeShade = Mathf.Lerp(0.94f, 1.04f, sideNoise);
            if (cell.Biome == BiomeType.Ocean || cell.Biome == BiomeType.Coast)
            {
                edgeShade = Mathf.Lerp(0.96f, 1.04f, sideNoise);
            }

            return ShadeColor(centerColor, edgeShade);
        }

        private static Color ShadeColor(Color color, float shade)
        {
            color.r = Mathf.Clamp01(color.r * shade);
            color.g = Mathf.Clamp01(color.g * shade);
            color.b = Mathf.Clamp01(color.b * shade);
            color.a = 1f;
            return color;
        }

        private SurfaceSample SampleVisualSurface(Vector3 normal, int seed)
        {
            var terrainSettings = TerrainSettings;
            var noiseSettings = NoiseSettings;
            var latitude = Mathf.Abs(normal.y);
            var continent = SphericalNoise(
                normal,
                seed,
                terrainSettings.ContinentSalt,
                terrainSettings.ContinentOctaves,
                terrainSettings.ContinentFrequency,
                noiseSettings);
            var detail = SphericalNoise(
                normal,
                seed,
                terrainSettings.DetailSalt,
                terrainSettings.DetailOctaves,
                terrainSettings.DetailFrequency,
                noiseSettings);
            var ridge = 1f - Mathf.Abs(SphericalNoise(
                normal,
                seed,
                terrainSettings.RidgeSalt,
                terrainSettings.RidgeOctaves,
                terrainSettings.RidgeFrequency,
                noiseSettings) * 2f - 1f);
            var rawHeight =
                continent * terrainSettings.ContinentWeight +
                detail * terrainSettings.DetailWeight +
                ridge * terrainSettings.RidgeWeight;
            var height = Mathf.Clamp01((rawHeight - terrainSettings.HeightContrastPivot) * terrainSettings.HeightContrast + terrainSettings.HeightOffset);
            var moistureNoise = SphericalNoise(
                normal,
                seed,
                terrainSettings.MoistureSalt,
                terrainSettings.MoistureOctaves,
                terrainSettings.MoistureFrequency,
                noiseSettings);
            var moisture = Mathf.Clamp01(moistureNoise * terrainSettings.MoistureNoiseWeight + latitude * terrainSettings.MoistureLatitudeWeight);
            var temperatureNoise = SphericalNoise(
                normal,
                seed,
                terrainSettings.TemperatureSalt,
                terrainSettings.TemperatureOctaves,
                terrainSettings.TemperatureFrequency,
                noiseSettings);
            var temperature = Mathf.Clamp01(
                terrainSettings.TemperatureBase -
                latitude * terrainSettings.TemperatureLatitudeWeight -
                height * terrainSettings.TemperatureHeightWeight +
                temperatureNoise * terrainSettings.TemperatureNoiseWeight);
            var biome = PickVisualBiome(height, moisture, temperature, ridge, normal, seed, terrainSettings);

            return new SurfaceSample
            {
                Biome = biome,
                Height = height,
                Color = GetVisualColor(biome, height, normal, seed, terrainSettings)
            };
        }

        private static BiomeType PickVisualBiome(
            float height,
            float moisture,
            float temperature,
            float ridge,
            Vector3 normal,
            int seed,
            WorldTerrainGenerationSettings terrainSettings)
        {
            if (height < terrainSettings.OceanThreshold)
            {
                return BiomeType.Ocean;
            }

            if (height < terrainSettings.CoastThreshold)
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

            var regionHash = Hash01(seed, Mathf.RoundToInt(normal.x * 997f + normal.y * 613f), Mathf.RoundToInt(normal.z * 431f));
            if (height > terrainSettings.RustDesertMinHeight &&
                moisture < terrainSettings.RustDesertMaxMoisture &&
                temperature > terrainSettings.RustDesertMinTemperature &&
                regionHash > terrainSettings.RustDesertChanceThreshold)
            {
                return BiomeType.RustDesert;
            }

            if (height > terrainSettings.AshWastesMinHeight &&
                moisture < terrainSettings.AshWastesMaxMoisture &&
                regionHash > terrainSettings.AshWastesChanceThreshold)
            {
                return BiomeType.AshWastes;
            }

            if (moisture > terrainSettings.SwampMinMoisture && height < terrainSettings.SwampMaxHeight)
            {
                return temperature > terrainSettings.ToxicSwampMinTemperature &&
                       regionHash > terrainSettings.ToxicSwampChanceThreshold
                    ? BiomeType.ToxicSwamp
                    : BiomeType.Swamp;
            }

            if (moisture > terrainSettings.ForestMinMoisture)
            {
                return temperature < terrainSettings.DeadForestMaxTemperature &&
                       regionHash > terrainSettings.DeadForestChanceThreshold
                    ? BiomeType.DeadForest
                    : BiomeType.Forest;
            }

            if (moisture < terrainSettings.DesertMaxMoisture && temperature > terrainSettings.DesertMinTemperature)
            {
                return BiomeType.Desert;
            }

            if (height > terrainSettings.IndustrialRuinsMinHeight && regionHash > terrainSettings.IndustrialRuinsChanceThreshold)
            {
                return BiomeType.IndustrialRuins;
            }

            if (height > terrainSettings.DemonScarMinHeight &&
                moisture < terrainSettings.DemonScarMaxMoisture &&
                regionHash > terrainSettings.DemonScarChanceThreshold)
            {
                return BiomeType.DemonScar;
            }

            return BiomeType.Plains;
        }

        private static Color GetVisualColor(
            BiomeType biome,
            float height,
            Vector3 normal,
            int seed,
            WorldTerrainGenerationSettings terrainSettings)
        {
            var color = GetBiomeColor(biome);
            if (height < terrainSettings.CoastThreshold + 0.12f)
            {
                var deepWater = new Color(0.06f, 0.14f, 0.26f, 1f);
                var shallowWater = new Color(0.16f, 0.31f, 0.44f, 1f);
                var shore = new Color(0.48f, 0.45f, 0.30f, 1f);
                var waterColor = Color.Lerp(deepWater, shallowWater, Mathf.SmoothStep(0.16f, terrainSettings.OceanThreshold + 0.08f, height));
                var shoreColor = Color.Lerp(shore, color, Mathf.SmoothStep(terrainSettings.CoastThreshold - 0.02f, terrainSettings.CoastThreshold + 0.12f, height));
                color = Color.Lerp(waterColor, shoreColor, Mathf.SmoothStep(terrainSettings.OceanThreshold - 0.05f, terrainSettings.CoastThreshold + 0.08f, height));
            }

            var reliefShade = Mathf.Lerp(0.9f, 1.08f, height);
            var broadNoise = Noise3(normal, seed, 10.5f, 19.7f);
            var fineNoise = Noise3(normal, seed, 38f, 71.1f);
            var textureShade = Mathf.Lerp(0.86f, 1.12f, broadNoise * 0.6f + fineNoise * 0.4f);

            if (biome == BiomeType.Ocean)
            {
                textureShade = Mathf.Lerp(0.92f, 1.08f, fineNoise);
                reliefShade = 1f;
            }
            else if (biome == BiomeType.Forest || biome == BiomeType.DeadForest)
            {
                textureShade = Mathf.Lerp(0.78f, 1.06f, fineNoise);
            }
            else if (biome == BiomeType.Mountains)
            {
                color = Color.Lerp(color, new Color(0.72f, 0.70f, 0.66f, 1f), Mathf.Clamp01((height - 0.62f) * 2.4f));
                textureShade = Mathf.Lerp(0.72f, 1.16f, fineNoise);
            }

            color.r = Mathf.Clamp01(color.r * reliefShade * textureShade);
            color.g = Mathf.Clamp01(color.g * reliefShade * textureShade);
            color.b = Mathf.Clamp01(color.b * reliefShade * textureShade);
            color.a = 1f;
            return color;
        }

        private static Color GetBiomeColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                    return new Color(0.08f, 0.18f, 0.31f, 1f);
                case BiomeType.Coast:
                    return new Color(0.43f, 0.43f, 0.24f, 1f);
                case BiomeType.Plains:
                    return new Color(0.42f, 0.41f, 0.20f, 1f);
                case BiomeType.Forest:
                    return new Color(0.20f, 0.31f, 0.14f, 1f);
                case BiomeType.Desert:
                    return new Color(0.58f, 0.44f, 0.31f, 1f);
                case BiomeType.Snow:
                    return new Color(0.78f, 0.80f, 0.76f, 1f);
                case BiomeType.Swamp:
                    return new Color(0.28f, 0.34f, 0.22f, 1f);
                case BiomeType.Mountains:
                    return new Color(0.46f, 0.42f, 0.36f, 1f);
                case BiomeType.AshWastes:
                    return new Color(0.25f, 0.23f, 0.22f, 1f);
                case BiomeType.RustDesert:
                    return new Color(0.55f, 0.35f, 0.25f, 1f);
                case BiomeType.DeadForest:
                    return new Color(0.29f, 0.32f, 0.22f, 1f);
                case BiomeType.IndustrialRuins:
                    return new Color(0.34f, 0.35f, 0.33f, 1f);
                case BiomeType.DemonScar:
                    return new Color(0.43f, 0.12f, 0.12f, 1f);
                case BiomeType.ToxicSwamp:
                    return new Color(0.31f, 0.42f, 0.16f, 1f);
                default:
                    return Color.magenta;
            }
        }

        private static float GetRoadWidth(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Large:
                    return 0.011f;
                case RoadType.Medium:
                    return 0.0075f;
                default:
                    return 0.0045f;
            }
        }

        private static Color GetRoadColor(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Large:
                    return LargeRoadColor;
                case RoadType.Medium:
                    return MediumRoadColor;
                default:
                    return SmallRoadColor;
            }
        }

        private static GlobalMapIconSpriteId GetActivityIconId(WorldActivityType activityType)
        {
            switch (activityType)
            {
                case WorldActivityType.Ruins:
                    return GlobalMapIconSpriteId.AncientRuins;
                case WorldActivityType.Mine:
                    return GlobalMapIconSpriteId.Mine;
                case WorldActivityType.CaravanStop:
                    return GlobalMapIconSpriteId.LogisticsStop;
                default:
                    return GlobalMapIconSpriteId.ExpeditionCamp;
            }
        }

        private static Color GetFactionMarkerColor(int factionId)
        {
            if (factionId < 0)
            {
                return new Color(0.68f, 0.72f, 0.78f, 1f);
            }

            return FactionMarkerColors[factionId % FactionMarkerColors.Length];
        }

        private static bool IsKnownFactionId(int factionId, IReadOnlyList<FactionData> factions)
        {
            for (var factionIndex = 0; factionIndex < factions.Count; factionIndex++)
            {
                if (factions[factionIndex].Id == factionId)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureRoot()
        {
            if (generatedRoot != null)
            {
                generatedRoot.gameObject.hideFlags = GetGeneratedHideFlags();
                return;
            }

            var existingRoot = transform.Find(GeneratedRootName);
            if (existingRoot != null)
            {
                generatedRoot = existingRoot;
                generatedRoot.gameObject.hideFlags = GetGeneratedHideFlags();
                return;
            }

            var root = CreateGeneratedGameObject(GeneratedRootName);
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;
        }

        private void DestroySelectionObject()
        {
            if (selectionObject == null && generatedRoot != null)
            {
                var existing = generatedRoot.Find(SelectionObjectName);
                if (existing != null)
                {
                    selectionObject = existing.gameObject;
                }
            }

            if (selectionObject == null)
            {
                return;
            }

            DestroyGeneratedObject(selectionObject);
            selectionObject = null;
        }

        private void EnsureMaterials()
        {
            vertexColorMaterial ??= CreateMaterial(new Color(1f, 1f, 1f, 1f), true);
            var iconTexture = artAtlas != null ? artAtlas.IconAtlasTexture : null;
            if (iconTexture == null)
            {
                DestroyObject(iconMaterial);
                iconMaterial = null;
                return;
            }

            if (iconMaterial != null && iconMaterial.mainTexture == iconTexture)
            {
                return;
            }

            DestroyObject(iconMaterial);
            iconMaterial = CreateTexturedMaterial(iconTexture);
        }

        private static Material CreateMaterial(Color color, bool vertexColor)
        {
            var shader = vertexColor
                ? FindShader("MercLord/GlobalMapVertexColor", "Sprites/Default", "Universal Render Pipeline/Unlit", "Standard")
                : FindShader("Sprites/Default", "Universal Render Pipeline/Unlit", "Standard");
            var material = new Material(shader)
            {
                color = color,
                hideFlags = GetGeneratedHideFlags()
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            SetEditorDirty(material);
            return material;
        }

        private static Material CreateTexturedMaterial(Texture texture)
        {
            var material = CreateMaterial(Color.white, false);
            material.mainTexture = texture;
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            SetEditorDirty(material);
            return material;
        }

        private static Mesh CreateMesh(string meshName, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            var mesh = new Mesh
            {
                name = meshName,
                hideFlags = GetGeneratedHideFlags()
            };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            SetEditorDirty(mesh);
            return mesh;
        }

        private void CreateGeneratedMeshObject(
            string objectName,
            string meshName,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<int> triangles,
            Material material)
        {
            if (vertices.Count == 0 || triangles.Count == 0)
            {
                return;
            }

            var mesh = CreateMesh(meshName, vertices, colors, triangles);
            if (normals.Count == vertices.Count)
            {
                mesh.SetNormals(normals);
            }

            mesh.RecalculateBounds();

            var meshObject = CreateGeneratedGameObject(objectName);
            meshObject.transform.SetParent(generatedRoot, false);
            var meshFilter = meshObject.AddComponent<MeshFilter>();
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void CreateGeneratedTexturedMeshObject(
            string objectName,
            string meshName,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Material material)
        {
            if (vertices.Count == 0 || triangles.Count == 0)
            {
                return;
            }

            var mesh = CreateMesh(meshName, vertices, colors, triangles);
            if (normals.Count == vertices.Count)
            {
                mesh.SetNormals(normals);
            }

            if (uvs.Count == vertices.Count)
            {
                mesh.SetUVs(0, uvs);
            }

            mesh.RecalculateBounds();

            var meshObject = CreateGeneratedGameObject(objectName);
            meshObject.transform.SetParent(generatedRoot, false);
            var meshFilter = meshObject.AddComponent<MeshFilter>();
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static GameObject CreateGeneratedGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName)
            {
                hideFlags = GetGeneratedHideFlags()
            };
            SetEditorDirty(gameObject);
            return gameObject;
        }

        private static HideFlags GetGeneratedHideFlags()
        {
            return Application.isPlaying ? RuntimeGeneratedHideFlags : HideFlags.None;
        }

        private static void SetEditorDirty(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && target != null)
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }

        private static Shader FindShader(params string[] names)
        {
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                var shader = Shader.Find(names[nameIndex]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return Shader.Find("Standard");
        }

        private static Vector3 ToVector(WorldSpherePoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }

        private static Vector3 SlerpNormal(Vector3 from, Vector3 to, float t)
        {
            var dot = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            if (dot > 0.9995f)
            {
                return Vector3.Lerp(from, to, t).normalized;
            }

            var theta = Mathf.Acos(dot);
            var sinTheta = Mathf.Sin(theta);
            var a = Mathf.Sin((1f - t) * theta) / sinTheta;
            var b = Mathf.Sin(t * theta) / sinTheta;
            return (from * a + to * b).normalized;
        }

        private static void GetMarkerBasis(Vector3 normal, out Vector3 tangentRight, out Vector3 tangentUp)
        {
            var upHint = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
            tangentRight = Vector3.Cross(upHint, normal).normalized;
            tangentUp = Vector3.Cross(normal, tangentRight).normalized;
        }

        private static Vector3 RandomUnitVector(System.Random random)
        {
            var z = (float)(random.NextDouble() * 2.0 - 1.0);
            var angle = (float)(random.NextDouble() * Math.PI * 2.0);
            var radius = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(Mathf.Cos(angle) * radius, z, Mathf.Sin(angle) * radius);
        }

        private static float SphericalNoise(
            Vector3 normal,
            int seed,
            int salt,
            int octaves,
            float baseFrequency,
            WorldNoiseSettings settings)
        {
            settings ??= WorldNoiseSettings.Default;
            var warpFrequency = settings.DomainWarpFrequency;
            var warpStrength = settings.DomainWarpStrength;
            var warpX = ValueNoise3D(normal.x * warpFrequency + 13.1f, normal.y * warpFrequency - 7.3f, normal.z * warpFrequency + 5.9f, seed, salt + 311);
            var warpY = ValueNoise3D(normal.x * warpFrequency - 3.7f, normal.y * warpFrequency + 17.1f, normal.z * warpFrequency - 11.4f, seed, salt + 719);
            var warpZ = ValueNoise3D(normal.x * warpFrequency + 23.8f, normal.y * warpFrequency + 2.6f, normal.z * warpFrequency - 19.2f, seed, salt + 1103);
            var x = normal.x + (warpX - 0.5f) * warpStrength;
            var y = normal.y + (warpY - 0.5f) * warpStrength;
            var z = normal.z + (warpZ - 0.5f) * warpStrength;
            var amplitude = 1f;
            var totalAmplitude = 0f;
            var value = 0f;
            var frequency = baseFrequency;
            for (var octave = 0; octave < octaves; octave++)
            {
                value += ValueNoise3D(x * frequency, y * frequency, z * frequency, seed, salt + octave * 193) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= settings.OctavePersistence;
                frequency *= settings.OctaveLacunarity;
            }

            return Mathf.Clamp01(value / totalAmplitude);
        }

        private static float ValueNoise3D(float x, float y, float z, int seed, int salt)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var ty = Smooth(y - y0);
            var tz = Smooth(z - z0);

            var x00 = Mathf.Lerp(Hash01(seed, salt, x0, y0, z0), Hash01(seed, salt, x0 + 1, y0, z0), tx);
            var x10 = Mathf.Lerp(Hash01(seed, salt, x0, y0 + 1, z0), Hash01(seed, salt, x0 + 1, y0 + 1, z0), tx);
            var x01 = Mathf.Lerp(Hash01(seed, salt, x0, y0, z0 + 1), Hash01(seed, salt, x0 + 1, y0, z0 + 1), tx);
            var x11 = Mathf.Lerp(Hash01(seed, salt, x0, y0 + 1, z0 + 1), Hash01(seed, salt, x0 + 1, y0 + 1, z0 + 1), tx);
            var y0Value = Mathf.Lerp(x00, x10, ty);
            var y1Value = Mathf.Lerp(x01, x11, ty);
            return Mathf.Lerp(y0Value, y1Value, tz);
        }

        private static float Smooth(float value)
        {
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private static float Hash01(int seed, int a, int b)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ a;
                hash = hash * 397 ^ b;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static float Hash01(int seed, int salt, int x, int y, int z)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ salt;
                hash = hash * 397 ^ x;
                hash = hash * 397 ^ y;
                hash = hash * 397 ^ z;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static float Noise3(Vector3 normal, int seed, float scale, float salt)
        {
            var offset = seed * 0.0137f + salt;
            var a = Mathf.PerlinNoise(normal.x * scale + offset, normal.y * scale - offset);
            var b = Mathf.PerlinNoise(normal.y * scale + offset * 1.31f, normal.z * scale + offset * 0.73f);
            var c = Mathf.PerlinNoise(normal.z * scale - offset * 0.47f, normal.x * scale + offset * 1.67f);
            return (a + b + c) / 3f;
        }

        private static bool IsCapitalCell(int cellId, FactionData[] factions)
        {
            for (var factionIndex = 0; factionIndex < factions.Length; factionIndex++)
            {
                if (factions[factionIndex].CapitalCellId == cellId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidEdge(int from, int to, int cellCount)
        {
            return IsValidCell(from, cellCount) && IsValidCell(to, cellCount);
        }

        private static bool IsValidCell(int cellId, int cellCount)
        {
            return cellId >= 0 && cellId < cellCount;
        }

        private void DestroyGeneratedObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            var meshFilters = target.GetComponentsInChildren<MeshFilter>();
            for (var meshIndex = 0; meshIndex < meshFilters.Length; meshIndex++)
            {
                DestroyObject(meshFilters[meshIndex].sharedMesh);
            }

            var meshRenderers = target.GetComponentsInChildren<MeshRenderer>();
            var materials = new HashSet<Material>();
            for (var rendererIndex = 0; rendererIndex < meshRenderers.Length; rendererIndex++)
            {
                var sharedMaterials = meshRenderers[rendererIndex].sharedMaterials;
                for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    var material = sharedMaterials[materialIndex];
                    if (material != null &&
                        material != vertexColorMaterial &&
                        material != iconMaterial &&
                        !IsPersistentAsset(material))
                    {
                        materials.Add(material);
                    }
                }
            }

            foreach (var material in materials)
            {
                DestroyObject(material);
            }

            DestroyObject(target);
        }

        private static bool IsPersistentAsset(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            return !Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(target);
#else
            return false;
#endif
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                DestroyObject(vertexColorMaterial);
                DestroyObject(iconMaterial);
            }
        }

        private struct SurfaceSample
        {
            public BiomeType Biome;
            public float Height;
            public Color Color;
        }
    }
}
