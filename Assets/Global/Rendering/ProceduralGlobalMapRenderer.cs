using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using UnityEngine;
using UnityEngine.Rendering;

namespace MercLord.Global.Rendering
{
    public sealed class ProceduralGlobalMapRenderer : MonoBehaviour
    {
        private const int NeighbourCount = 6;
#if UNITY_EDITOR
        private const string GeneratedRootName = "Generated Map";
        private const string SelectionObjectName = "Selected Cell Highlight";
        private const string MarkerIconsObjectName = "Marker Icons Mesh";
        private const string SettlementFeaturesObjectName = "Settlement Feature Textures Mesh";
        private const string ActivityFeaturesObjectName = "Activity Feature Textures Mesh";
        private const string LegacyMarkersObjectName = "Markers Mesh";
#endif
        private const HideFlags RuntimeGeneratedHideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        private const float HexAreaFactor = 2.598076f;
        private const int SurfaceBlendCandidateCount = 2;
        private const int SurfaceBlendMaxBinSearchRadius = 3;

        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private GlobalMapArtAtlas artAtlas;
        [NonSerialized] private WorldModel currentWorld;

        [Header("Render Layers")]
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private GlobalMapProceduralRenderSettings renderSettings;
        [SerializeField] private GlobalMapMeshLayer starfieldLayer;
        [SerializeField] private GlobalMapMeshLayer terrainLayer;
        [SerializeField] private GlobalMapMeshLayer riversLayer;
        [SerializeField] private GlobalMapMeshLayer roadsLayer;
        [SerializeField] private GlobalMapMeshLayer markerIconsLayer;
        [SerializeField] private GlobalMapMeshLayer settlementFeaturesLayer;
        [SerializeField] private GlobalMapMeshLayer activityFeaturesLayer;
        [SerializeField] private GlobalMapMeshLayer selectionLayer;

        private Material vertexColorMaterial;
        private Material terrainMaterial;
        private Material iconMaterial;
        private Material settlementFeatureMaterial;
        private Material activityFeatureMaterial;
        private Material vertexColorMaterialTemplateSource;
        private Material terrainMaterialTemplateSource;
        private Material iconMaterialTemplateSource;
        private Material settlementFeatureMaterialTemplateSource;
        private Material activityFeatureMaterialTemplateSource;
        private Texture2D terrainSurfaceTexture;
        private bool markerIconsVisible = true;
        private bool featureTexturesVisible;
        private int selectedCellId = WorldIds.None;

        public WorldModel CurrentWorld => currentWorld;
        public GlobalMapArtAtlas ArtAtlas => artAtlas;
        public int SelectedCellId => selectedCellId;
        public float PlanetRadius => RenderSettings.PlanetRadius;
        private WorldTerrainGenerationSettings TerrainSettings => configDatabase?.GlobalGeneration?.Terrain ?? WorldTerrainGenerationSettings.Default;
        private WorldNoiseSettings NoiseSettings => configDatabase?.GlobalGeneration?.Noise ?? WorldNoiseSettings.Default;
        private GlobalMapProceduralRenderSettings RenderSettings =>
            renderSettings != null
                ? renderSettings
                : throw new InvalidOperationException("ProceduralGlobalMapRenderer requires GlobalMapProceduralRenderSettings.");

        public void Configure(ConfigDatabase database)
        {
            configDatabase = database;
        }

        public void ConfigureArtAtlas(GlobalMapArtAtlas atlas)
        {
            artAtlas = atlas;
        }

        public void SetMarkerIconsVisible(bool visible)
        {
            markerIconsVisible = visible;
            featureTexturesVisible = !visible;
            ApplyMapPointLayerVisibility();
        }

        public void Render(WorldModel worldModel)
        {
            Render(worldModel, false);
        }

        public void Render(WorldModel worldModel, bool finalQuality)
        {
            if (worldModel == null)
            {
                throw new ArgumentNullException(nameof(worldModel));
            }

            RenderShared(worldModel);
            RenderTerrain(worldModel, finalQuality);
            RenderDynamicLayers(worldModel);
        }

        public async UniTask RenderAsync(
            WorldModel worldModel,
            bool finalQuality = false,
            CancellationToken cancellationToken = default)
        {
            if (worldModel == null)
            {
                throw new ArgumentNullException(nameof(worldModel));
            }

            cancellationToken.ThrowIfCancellationRequested();
            RenderShared(worldModel);
            await RenderTerrainAsync(worldModel, finalQuality, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            RenderDynamicLayers(worldModel);
        }

        private void RenderShared(WorldModel worldModel)
        {
            currentWorld = worldModel;
            EnsureRenderLayers();
            ClearGenerated();
            EnsureMaterials();
            RenderStarfield(worldModel.Seed);
        }

        private void RenderDynamicLayers(WorldModel worldModel)
        {
            RenderRivers(worldModel);
            RenderRoads(worldModel);
            RenderMarkers(worldModel);
            RenderFeatureTextures(worldModel);
        }

        public void ClearGenerated()
        {
            EnsureRenderLayers();
            ClearLayer(starfieldLayer);
            ClearLayer(terrainLayer);
            ClearLayer(riversLayer);
            ClearLayer(roadsLayer);
            ClearLayer(markerIconsLayer);
            ClearLayer(settlementFeaturesLayer);
            ClearLayer(activityFeaturesLayer);
            ClearLayer(selectionLayer);

            DestroyUnityObject(vertexColorMaterial);
            vertexColorMaterial = null;
            vertexColorMaterialTemplateSource = null;
            DestroyUnityObject(terrainMaterial);
            terrainMaterial = null;
            terrainMaterialTemplateSource = null;
            DestroyUnityObject(terrainSurfaceTexture);
            terrainSurfaceTexture = null;
            DestroyUnityObject(iconMaterial);
            iconMaterial = null;
            iconMaterialTemplateSource = null;
            DestroyUnityObject(settlementFeatureMaterial);
            settlementFeatureMaterial = null;
            settlementFeatureMaterialTemplateSource = null;
            DestroyUnityObject(activityFeatureMaterial);
            activityFeatureMaterial = null;
            activityFeatureMaterialTemplateSource = null;
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
            ClearLayer(selectionLayer);
        }

        private void RenderStarfield(int seed)
        {
            var settings = RenderSettings;
            var random = new System.Random(seed ^ 0x45C1A3D);
            var starCount = settings.StarCount;
            var vertices = new List<Vector3>(starCount * 4);
            var colors = new List<Color>(starCount * 4);
            var triangles = new List<int>(starCount * 6);

            for (var starIndex = 0; starIndex < starCount; starIndex++)
            {
                var normal = RandomUnitVector(random);
                var tangent = Vector3.Cross(Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up, normal).normalized;
                var bitangent = Vector3.Cross(normal, tangent).normalized;
                var center = normal * settings.StarfieldRadius;
                var size = Mathf.Lerp(settings.StarSizeRange.x, settings.StarSizeRange.y, (float)random.NextDouble());
                var brightness = Mathf.Lerp(settings.StarBrightnessRange.x, settings.StarBrightnessRange.y, (float)random.NextDouble());
                var tint = Color.Lerp(settings.StarTint, Color.white, (float)random.NextDouble());
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
            SetGeneratedMesh(starfieldLayer, mesh, vertexColorMaterial);
        }

        private void RenderTerrain(WorldModel worldModel, bool finalQuality)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            if (cells.Length == 0)
            {
                return;
            }

            var settings = RenderSettings;
            var lonSegments = GetTerrainSurfaceLongitudeSegments(settings, finalQuality);
            var latSegments = GetTerrainSurfaceLatitudeSegments(settings, finalQuality);
            var vertices = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var normals = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var colors = new List<Color>((latSegments + 1) * (lonSegments + 1));
            var uvs = new List<Vector2>((latSegments + 1) * (lonSegments + 1));
            var triangles = new List<int>(latSegments * lonSegments * 6);
            var lookup = new DirectionCellLookup(cells, lonSegments, latSegments);
            var texture = BuildTerrainSurfaceTexture(worldModel.Seed, lookup, settings, finalQuality);
            BuildTerrainSurfaceMesh(settings, finalQuality, vertices, normals, colors, uvs, triangles);
            SetGeneratedTexturedMesh(terrainLayer, "GlobalMap Continuous Terrain", vertices, normals, colors, uvs, triangles, EnsureTerrainMaterial(texture));
        }

        private async UniTask RenderTerrainAsync(
            WorldModel worldModel,
            bool finalQuality,
            CancellationToken cancellationToken)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            if (cells.Length == 0)
            {
                return;
            }

            var settings = RenderSettings;
            var lonSegments = GetTerrainSurfaceLongitudeSegments(settings, finalQuality);
            var latSegments = GetTerrainSurfaceLatitudeSegments(settings, finalQuality);
            var vertices = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var normals = new List<Vector3>((latSegments + 1) * (lonSegments + 1));
            var colors = new List<Color>((latSegments + 1) * (lonSegments + 1));
            var uvs = new List<Vector2>((latSegments + 1) * (lonSegments + 1));
            var triangles = new List<int>(latSegments * lonSegments * 6);
            var lookup = new DirectionCellLookup(cells, lonSegments, latSegments);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var texture = BuildTerrainSurfaceTexture(worldModel.Seed, lookup, settings, finalQuality);
            cancellationToken.ThrowIfCancellationRequested();
            BuildTerrainSurfaceMesh(settings, finalQuality, vertices, normals, colors, uvs, triangles);
            SetGeneratedTexturedMesh(terrainLayer, "GlobalMap Continuous Terrain", vertices, normals, colors, uvs, triangles, EnsureTerrainMaterial(texture));
        }

        private void BuildTerrainSurfaceMesh(
            GlobalMapProceduralRenderSettings settings,
            bool finalQuality,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles)
        {
            var lonSegments = GetTerrainSurfaceLongitudeSegments(settings, finalQuality);
            var latSegments = GetTerrainSurfaceLatitudeSegments(settings, finalQuality);
            var terrainRadius = settings.PlanetRadius + settings.TerrainSurfaceOffset;
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
                    vertices.Add(normal * terrainRadius);
                    normals.Add(normal);
                    colors.Add(Color.white);
                    uvs.Add(new Vector2(u, 1f - v));
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

                    triangles.Add(d);
                    triangles.Add(b);
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(c);
                    triangles.Add(b);
                }
            }
        }

        private static int GetTerrainSurfaceLongitudeSegments(GlobalMapProceduralRenderSettings settings, bool finalQuality)
        {
            return finalQuality ? settings.TerrainSurfaceLongitudeSegments : settings.PreviewTerrainSurfaceLongitudeSegments;
        }

        private static int GetTerrainSurfaceLatitudeSegments(GlobalMapProceduralRenderSettings settings, bool finalQuality)
        {
            return finalQuality ? settings.TerrainSurfaceLatitudeSegments : settings.PreviewTerrainSurfaceLatitudeSegments;
        }

        private Texture2D BuildTerrainSurfaceTexture(
            int seed,
            DirectionCellLookup lookup,
            GlobalMapProceduralRenderSettings settings,
            bool finalQuality)
        {
            var width = finalQuality ? settings.TerrainTextureWidth : settings.PreviewTerrainTextureWidth;
            var height = finalQuality ? settings.TerrainTextureHeight : settings.PreviewTerrainTextureHeight;
            var pixels = new Color32[width * height];
            var terrainSettings = TerrainSettings;
            var noiseSettings = NoiseSettings;
            var palette = CreateTerrainVisualPalette();
            var boundaryBlendRadians = settings.TerrainBiomeBlendPixels * Mathf.PI * 2f / Mathf.Max(1, width);

            for (var y = 0; y < height; y++)
            {
                var latitude01 = (y + 0.5f) / height;
                var normalY = Mathf.Sin((latitude01 - 0.5f) * Mathf.PI);
                var ringRadius = Mathf.Sqrt(Mathf.Max(0f, 1f - normalY * normalY));
                for (var x = 0; x < width; x++)
                {
                    var longitude01 = (x + 0.5f) / width;
                    var azimuth = longitude01 * Mathf.PI * 2f;
                    var normal = new Vector3(Mathf.Cos(azimuth) * ringRadius, normalY, Mathf.Sin(azimuth) * ringRadius);
                    pixels[y * width + x] = SampleWorldSurfaceColor(
                        normal,
                        seed,
                        lookup,
                        terrainSettings,
                        noiseSettings,
                        palette,
                        boundaryBlendRadians);
                }
            }

            DestroyUnityObject(terrainSurfaceTexture);
            terrainSurfaceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "GlobalMap Terrain Surface",
                hideFlags = GetGeneratedHideFlags(),
                filterMode = FilterMode.Point,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                anisoLevel = 1
            };
            terrainSurfaceTexture.SetPixels32(pixels);
            terrainSurfaceTexture.Apply(false, false);
            SetEditorDirty(terrainSurfaceTexture);
            return terrainSurfaceTexture;
        }

        private void RenderRivers(WorldModel worldModel)
        {
            var settings = RenderSettings;
            var riverEdges = worldModel.RiverEdges ?? Array.Empty<WorldRiverEdge>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var lineSegments = settings.LineSegments;
            var vertices = new List<Vector3>(riverEdges.Length * lineSegments * 4);
            var normals = new List<Vector3>(riverEdges.Length * lineSegments * 4);
            var colors = new List<Color>(riverEdges.Length * lineSegments * 4);
            var triangles = new List<int>(riverEdges.Length * lineSegments * 6);

            for (var edgeIndex = 0; edgeIndex < riverEdges.Length; edgeIndex++)
            {
                var edge = riverEdges[edgeIndex];
                if (!IsValidEdge(edge.FromCellId, edge.ToCellId, cells.Length))
                {
                    continue;
                }

                var flowWidth = Mathf.Sqrt(Mathf.Max(0f, edge.Flow));
                var width = settings.RiverBaseWidth +
                            Mathf.Clamp(flowWidth, 0f, settings.RiverMaxFlowWidth) *
                            settings.RiverFlowWidthMultiplier;
                AddSphericalRibbon(
                    vertices,
                    normals,
                    colors,
                    triangles,
                    cells[edge.FromCellId],
                    cells[edge.ToCellId],
                    width,
                    settings.RiverSurfaceOffset,
                    settings.RiverColor);
            }

            SetGeneratedMesh(riversLayer, "GlobalMap Rivers", vertices, normals, colors, triangles, vertexColorMaterial);
        }

        private void RenderRoads(WorldModel worldModel)
        {
            var settings = RenderSettings;
            var roadEdges = worldModel.RoadEdges ?? Array.Empty<WorldRoadEdge>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var lineSegments = settings.LineSegments;
            var vertices = new List<Vector3>(roadEdges.Length * lineSegments * 4);
            var normals = new List<Vector3>(roadEdges.Length * lineSegments * 4);
            var colors = new List<Color>(roadEdges.Length * lineSegments * 4);
            var triangles = new List<int>(roadEdges.Length * lineSegments * 6);

            for (var edgeIndex = 0; edgeIndex < roadEdges.Length; edgeIndex++)
            {
                var edge = roadEdges[edgeIndex];
                if (!IsValidEdge(edge.FromCellId, edge.ToCellId, cells.Length))
                {
                    continue;
                }

                var width = settings.GetRoadWidth(edge.RoadType);
                AddSphericalRibbon(
                    vertices,
                    normals,
                    colors,
                    triangles,
                    cells[edge.FromCellId],
                    cells[edge.ToCellId],
                    width,
                    settings.RoadSurfaceOffset,
                    settings.GetRoadColor(edge.RoadType));
            }

            SetGeneratedMesh(roadsLayer, "GlobalMap Roads", vertices, normals, colors, triangles, vertexColorMaterial);
        }

        private void RenderMarkers(WorldModel worldModel)
        {
            if (RenderTexturedMarkers(worldModel))
            {
                return;
            }

            var settings = RenderSettings;
            var settlements = worldModel.Settlements ?? Array.Empty<SettlementData>();
            var activities = worldModel.Activities ?? Array.Empty<WorldActivityData>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var factions = worldModel.Factions ?? Array.Empty<FactionData>();
            var settlementShape = settings.LegacySettlementShape;
            var activityShape = settings.LegacyActivityShape;
            var caravanStopShape = settings.LegacyCaravanStopShape;
            var markerVertexCapacity = settlements.Length * (settlementShape.Count + 1) * 2 +
                                       activities.Length * (activityShape.Count + 1) * 2;
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
                var size = isCapital ? settings.LegacyCapitalMarkerSize : settings.LegacySettlementMarkerSize;
                AddFlatIcon(vertices, normals, colors, triangles, cells[settlement.CellId], settlementShape, size, color, settings.LegacySettlementSurfaceOffset);
            }

            for (var activityIndex = 0; activityIndex < activities.Length; activityIndex++)
            {
                var activity = activities[activityIndex];
                if (!IsValidCell(activity.CellId, cells.Length))
                {
                    continue;
                }

                var shape = activity.Type == WorldActivityType.CaravanStop ? caravanStopShape : activityShape;
                var factionId = IsKnownFactionId(activity.FactionId, factions)
                    ? activity.FactionId
                    : cells[activity.CellId].OwnerFactionId;
                var color = GetFactionMarkerColor(factionId);
                AddFlatIcon(vertices, normals, colors, triangles, cells[activity.CellId], shape, settings.LegacyActivityMarkerSize, color, settings.LegacyActivitySurfaceOffset);
            }

            SetGeneratedMesh(markerIconsLayer, "GlobalMap Markers", vertices, normals, colors, triangles, vertexColorMaterial);
            ApplyMapPointLayerVisibility();
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

            var settings = RenderSettings;
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

                var size = iconId == GlobalMapIconSpriteId.Capital ? settings.CapitalMarkerIconSize : settings.SettlementMarkerIconSize;
                AddSpriteMarker(vertices, normals, colors, uvs, triangles, cells[settlement.CellId], sprite, size, Color.white, settings.SettlementIconSurfaceOffset);
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

                AddSpriteMarker(vertices, normals, colors, uvs, triangles, cells[activity.CellId], sprite, settings.ActivityMarkerIconSize, Color.white, settings.ActivityIconSurfaceOffset);
            }

            SetGeneratedTexturedMesh(markerIconsLayer, "GlobalMap Marker Icons", vertices, normals, colors, uvs, triangles, iconMaterial);
            ApplyMapPointLayerVisibility();
            return markerIconsLayer != null && markerIconsLayer.HasMesh;
        }

        private void RenderFeatureTextures(WorldModel worldModel)
        {
            RenderSettlementFeatureTextures(worldModel);
            RenderActivityFeatureTextures(worldModel);
            ApplyMapPointLayerVisibility();
        }

        private void RenderSettlementFeatureTextures(WorldModel worldModel)
        {
            if (artAtlas == null ||
                artAtlas.SettlementFeatureAtlasTexture == null ||
                artAtlas.SettlementFeatureSprites == null ||
                artAtlas.SettlementFeatureSprites.Length == 0 ||
                settlementFeatureMaterial == null)
            {
                ClearLayer(settlementFeaturesLayer);
                return;
            }

            var settings = RenderSettings;
            var settlements = worldModel.Settlements ?? Array.Empty<SettlementData>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var factions = worldModel.Factions ?? Array.Empty<FactionData>();
            var neighbours = worldModel.Neighbours ?? Array.Empty<CellNeighbours>();
            const int expectedVerticesPerTile = 7;
            var vertices = new List<Vector3>(settlements.Length * expectedVerticesPerTile);
            var normals = new List<Vector3>(settlements.Length * expectedVerticesPerTile);
            var colors = new List<Color>(settlements.Length * expectedVerticesPerTile);
            var uvs = new List<Vector2>(settlements.Length * expectedVerticesPerTile);
            var triangles = new List<int>(settlements.Length * NeighbourCount * 3);
            var directNeighbours = new List<int>(NeighbourCount);
            var cornerNormals = new List<Vector3>(NeighbourCount);

            for (var settlementIndex = 0; settlementIndex < settlements.Length; settlementIndex++)
            {
                var settlement = settlements[settlementIndex];
                if (!IsValidCell(settlement.CellId, cells.Length))
                {
                    continue;
                }

                var spriteId = GetSettlementFeatureSpriteId(settlement, factions);
                if (!artAtlas.TryGetSettlementFeatureSprite(spriteId, out var sprite))
                {
                    continue;
                }

                var tintStrength = spriteId == GlobalMapSettlementFeatureSpriteId.Level5 ? 0.18f : 0.08f;
                var tint = Color.Lerp(Color.white, GetFactionMarkerColor(settlement.FactionId), tintStrength);
                tint.a = 1f;
                AddCellSpriteTile(vertices, normals, colors, uvs, triangles, cells, neighbours, settlement.CellId, sprite, tint, settings.SettlementFeatureSurfaceOffset, directNeighbours, cornerNormals);
            }

            SetGeneratedTexturedMesh(settlementFeaturesLayer, "GlobalMap Settlement Features", vertices, normals, colors, uvs, triangles, settlementFeatureMaterial);
        }

        private void RenderActivityFeatureTextures(WorldModel worldModel)
        {
            if (artAtlas == null ||
                artAtlas.ActivityFeatureAtlasTexture == null ||
                artAtlas.ActivityFeatureSprites == null ||
                artAtlas.ActivityFeatureSprites.Length == 0 ||
                activityFeatureMaterial == null)
            {
                ClearLayer(activityFeaturesLayer);
                return;
            }

            var settings = RenderSettings;
            var activities = worldModel.Activities ?? Array.Empty<WorldActivityData>();
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var neighbours = worldModel.Neighbours ?? Array.Empty<CellNeighbours>();
            const int expectedVerticesPerTile = 7;
            var vertices = new List<Vector3>(activities.Length * expectedVerticesPerTile);
            var normals = new List<Vector3>(activities.Length * expectedVerticesPerTile);
            var colors = new List<Color>(activities.Length * expectedVerticesPerTile);
            var uvs = new List<Vector2>(activities.Length * expectedVerticesPerTile);
            var triangles = new List<int>(activities.Length * NeighbourCount * 3);
            var directNeighbours = new List<int>(NeighbourCount);
            var cornerNormals = new List<Vector3>(NeighbourCount);

            for (var activityIndex = 0; activityIndex < activities.Length; activityIndex++)
            {
                var activity = activities[activityIndex];
                if (!IsValidCell(activity.CellId, cells.Length))
                {
                    continue;
                }

                if (!artAtlas.TryGetActivityFeatureSprite(GetActivityIconId(activity.Type), out var sprite))
                {
                    continue;
                }

                AddCellSpriteTile(vertices, normals, colors, uvs, triangles, cells, neighbours, activity.CellId, sprite, Color.white, settings.ActivityFeatureSurfaceOffset, directNeighbours, cornerNormals);
            }

            SetGeneratedTexturedMesh(activityFeaturesLayer, "GlobalMap Activity Features", vertices, normals, colors, uvs, triangles, activityFeatureMaterial);
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

            EnsureRenderLayers();
            EnsureMaterials();
            ClearLayer(selectionLayer);

            var cell = cells[selectedCellId];
            var normal = ToVector(cell.SpherePosition).normalized;
            var directNeighbours = new List<int>(NeighbourCount);
            var cornerNormals = new List<Vector3>(NeighbourCount);
            BuildGeodesicTileCornerNormals(cells, neighbours, selectedCellId, directNeighbours, cornerNormals);
            if (cornerNormals.Count < 3)
            {
                return;
            }

            var settings = RenderSettings;
            var terrainRadius = settings.PlanetRadius + settings.SelectionSurfaceOffset;
            var insetRatio = settings.SelectionInsetRatio;
            var vertices = new List<Vector3>(cornerNormals.Count + 1);
            var normals = new List<Vector3>(cornerNormals.Count + 1);
            var colors = new List<Color>(cornerNormals.Count + 1);
            var triangles = new List<int>(cornerNormals.Count * 3);
            var centerIndex = vertices.Count;
            vertices.Add(normal * terrainRadius);
            normals.Add(normal);
            colors.Add(settings.SelectionColor);

            for (var vertexIndex = 0; vertexIndex < cornerNormals.Count; vertexIndex++)
            {
                var ringNormal = SlerpNormalUnclamped(normal, cornerNormals[vertexIndex], insetRatio);
                vertices.Add(ringNormal * terrainRadius);
                normals.Add(ringNormal);
                colors.Add(settings.SelectionColor);
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

            SetGeneratedMesh(selectionLayer, mesh, vertexColorMaterial);
        }

        private bool TryRaycastPlanet(Ray worldRay, out Vector3 hitNormal)
        {
            var localOrigin = transform.InverseTransformPoint(worldRay.origin);
            var localDirection = transform.InverseTransformDirection(worldRay.direction).normalized;
            var settings = RenderSettings;
            var radius = settings.PlanetRadius + settings.SelectionSurfaceOffset;
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
            var radius = RenderSettings.PlanetRadius + offset;
            var halfWidth = width * 0.5f;

            var lineSegments = RenderSettings.LineSegments;
            for (var segment = 0; segment < lineSegments; segment++)
            {
                var t = segment / (float)lineSegments;
                var nextT = (segment + 1) / (float)lineSegments;
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

            var settings = RenderSettings;
            AddFlatIconPart(vertices, normals, colors, triangles, shape, size * settings.LegacyFlatIconOutlineScale, Color.black, position, normal, tangentRight, tangentUp);
            AddFlatIconPart(vertices, normals, colors, triangles, shape, size, color, normal * settings.LegacyFlatIconNudge + position, normal, tangentRight, tangentUp);
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
            var nudgedPosition = position + normal * RenderSettings.SpriteMarkerNudge;

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

        private void AddCellSpriteTile(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            WorldCell[] cells,
            CellNeighbours[] neighbours,
            int cellId,
            Sprite sprite,
            Color color,
            float offset,
            List<int> directNeighbours,
            List<Vector3> cornerNormals)
        {
            if (!IsValidCell(cellId, cells.Length) || sprite == null || sprite.texture == null)
            {
                return;
            }

            BuildGeodesicTileCornerNormals(cells, neighbours, cellId, directNeighbours, cornerNormals);
            if (cornerNormals.Count < 3)
            {
                return;
            }

            var cell = cells[cellId];
            var centerNormal = ToVector(cell.SpherePosition).normalized;
            var radius = RenderSettings.PlanetRadius + offset;
            GetMarkerBasis(centerNormal, out var tangentRight, out var tangentUp);
            var maxProjection = GetMaxCornerProjection(cornerNormals, tangentRight, tangentUp);

            var centerIndex = vertices.Count;
            vertices.Add(centerNormal * radius);
            normals.Add(centerNormal);
            colors.Add(color);
            uvs.Add(GetSpriteUv(sprite, new Vector2(0.5f, 0.5f)));

            for (var cornerIndex = 0; cornerIndex < cornerNormals.Count; cornerIndex++)
            {
                var cornerNormal = cornerNormals[cornerIndex];
                vertices.Add(cornerNormal * radius);
                normals.Add(cornerNormal);
                colors.Add(color);
                uvs.Add(GetSpriteUv(
                    sprite,
                    GetBiomeTileUv(
                        cornerNormal,
                        tangentRight,
                        tangentUp,
                        maxProjection)));
            }

            for (var cornerIndex = 0; cornerIndex < cornerNormals.Count; cornerIndex++)
            {
                triangles.Add(centerIndex);
                triangles.Add(centerIndex + 1 + cornerIndex);
                triangles.Add(centerIndex + 1 + ((cornerIndex + 1) % cornerNormals.Count));
            }
        }

        private Vector3 GetCellPosition(WorldCell cell, float offset)
        {
            return ToVector(cell.SpherePosition).normalized * GetSurfaceRadius(cell, offset);
        }

        private float GetSurfaceRadius(WorldCell cell, float offset)
        {
            return RenderSettings.PlanetRadius + offset;
        }

        private float GetSurfaceRadius(SurfaceSample sample, float offset)
        {
            return RenderSettings.PlanetRadius + offset;
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
            SortNeighboursAroundCenter(cells, centerNormal, directNeighbours);
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

        private static void SortNeighboursAroundCenter(
            WorldCell[] cells,
            Vector3 centerNormal,
            List<int> directNeighbours)
        {
            GetMarkerBasis(centerNormal, out var tangentRight, out var tangentUp);
            for (var index = 1; index < directNeighbours.Count; index++)
            {
                var neighbourId = directNeighbours[index];
                var angle = GetNeighbourAngle(cells, neighbourId, tangentRight, tangentUp);
                var previousIndex = index - 1;
                while (previousIndex >= 0 &&
                       GetNeighbourAngle(cells, directNeighbours[previousIndex], tangentRight, tangentUp) > angle)
                {
                    directNeighbours[previousIndex + 1] = directNeighbours[previousIndex];
                    previousIndex--;
                }

                directNeighbours[previousIndex + 1] = neighbourId;
            }
        }

        private static float GetNeighbourAngle(
            WorldCell[] cells,
            int cellId,
            Vector3 tangentRight,
            Vector3 tangentUp)
        {
            var normal = ToVector(cells[cellId].SpherePosition).normalized;
            return Mathf.Atan2(Vector3.Dot(normal, tangentUp), Vector3.Dot(normal, tangentRight));
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

            CreateSeedPolygon(Mathf.Max(fallbackRadius, seedRadius * RenderSettings.TileVoronoiSeedRadiusMultiplier), polygon);
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
            var vertexCount = RenderSettings.TileVoronoiSeedVertexCount;
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

        private Color SampleWorldSurfaceColor(
            Vector3 normal,
            int seed,
            DirectionCellLookup lookup,
            WorldTerrainGenerationSettings terrainSettings,
            WorldNoiseSettings noiseSettings,
            TerrainVisualPalette palette,
            float boundaryBlendRadians)
        {
            var candidateCount = lookup.FindNearest(normal);
            if (candidateCount == 0)
            {
                return SampleVisualSurface(normal, seed, terrainSettings, noiseSettings, palette).Color;
            }

            var primaryCell = lookup.GetCandidateCell(0);
            var color = GetVisualColor(primaryCell.Biome, primaryCell.Height, normal, seed, terrainSettings, palette);
            var blendWeight = 0f;
            for (var candidateIndex = 1; candidateIndex < candidateCount; candidateIndex++)
            {
                var cell = lookup.GetCandidateCell(candidateIndex);
                if (cell.Biome == primaryCell.Biome)
                {
                    continue;
                }

                var dot = Mathf.Clamp(lookup.GetCandidateDot(candidateIndex), -1f, 1f);
                var primaryDot = Mathf.Clamp(lookup.GetCandidateDot(0), -1f, 1f);
                var primaryAngle = Mathf.Acos(primaryDot);
                var candidateAngle = Mathf.Acos(dot);
                var boundaryDistance = Mathf.Max(0f, candidateAngle - primaryAngle);
                var edgeBlend = Mathf.SmoothStep(1f, 0f, boundaryDistance / Mathf.Max(0.000001f, boundaryBlendRadians));
                if (edgeBlend <= 0f)
                {
                    continue;
                }

                var weight = edgeBlend * 0.5f;
                color = Color.Lerp(color, GetVisualColor(cell.Biome, cell.Height, normal, seed, terrainSettings, palette), weight);
                blendWeight += weight;
            }

            color = AddSurfaceMicroDetail(color, normal, seed, primaryCell.Height, blendWeight, primaryCell.Biome);
            color.a = 1f;
            return color;
        }

        private static Color AddSurfaceMicroDetail(
            Color color,
            Vector3 normal,
            int seed,
            float height,
            float blendWeight,
            BiomeType biome)
        {
            var boundaryFade = 1f - Mathf.Clamp01(blendWeight * 2f);
            if (boundaryFade <= 0.001f)
            {
                return color;
            }

            var fine = Noise3(normal, seed, 118f, 137.9f) - 0.5f;
            var grain = Noise3(normal, seed, 276f, 311.7f) - 0.5f;
            var detail = fine * 0.72f + grain * 0.28f;
            var strength = GetSurfaceMicroDetailStrength(biome) * boundaryFade;
            var shade = 1f + detail * strength;
            var relief = 1f + (height - 0.5f) * 0.035f * boundaryFade;
            color.r = Mathf.Clamp01(color.r * shade * relief);
            color.g = Mathf.Clamp01(color.g * shade * relief);
            color.b = Mathf.Clamp01(color.b * shade * relief);
            color.a = 1f;
            return color;
        }

        private static float GetSurfaceMicroDetailStrength(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                case BiomeType.Coast:
                    return 0.035f;
                case BiomeType.Mountains:
                    return 0.085f;
                case BiomeType.Forest:
                case BiomeType.DeadForest:
                    return 0.065f;
                case BiomeType.Snow:
                    return 0.030f;
                default:
                    return 0.050f;
            }
        }

        private static float GetMaxCornerProjection(
            IReadOnlyList<Vector3> cornerNormals,
            Vector3 tangentRight,
            Vector3 tangentUp)
        {
            var maxProjection = 0f;
            for (var cornerIndex = 0; cornerIndex < cornerNormals.Count; cornerIndex++)
            {
                var corner = cornerNormals[cornerIndex];
                var projected = new Vector2(
                    Vector3.Dot(corner, tangentRight),
                    Vector3.Dot(corner, tangentUp));
                maxProjection = Mathf.Max(maxProjection, projected.magnitude);
            }

            return Mathf.Max(maxProjection, 0.000001f);
        }

        private static Vector2 GetBiomeTileUv(
            Vector3 ringNormal,
            Vector3 tangentRight,
            Vector3 tangentUp,
            float maxProjection)
        {
            var local = new Vector2(
                Vector3.Dot(ringNormal, tangentRight),
                Vector3.Dot(ringNormal, tangentUp)) / Mathf.Max(0.000001f, maxProjection);
            local = Vector2.ClampMagnitude(local, 1f);
            return new Vector2(
                Mathf.Clamp01(0.5f + local.x * 0.5f),
                Mathf.Clamp01(0.5f + local.y * 0.5f));
        }

        private Vector2 GetSpriteUv(Sprite sprite, Vector2 localUv)
        {
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            var uvPadding = RenderSettings.BiomeTileUvPadding;
            var paddingX = Mathf.Min(rect.width * uvPadding, rect.width * 0.45f);
            var paddingY = Mathf.Min(rect.height * uvPadding, rect.height * 0.45f);
            var x = Mathf.Lerp(rect.xMin + paddingX, rect.xMax - paddingX, Mathf.Clamp01(localUv.x));
            var y = Mathf.Lerp(rect.yMin + paddingY, rect.yMax - paddingY, Mathf.Clamp01(localUv.y));
            return new Vector2(x / texture.width, y / texture.height);
        }

        private static SurfaceSample SampleVisualSurface(
            Vector3 normal,
            int seed,
            WorldTerrainGenerationSettings terrainSettings,
            WorldNoiseSettings noiseSettings,
            TerrainVisualPalette palette)
        {
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
                Color = GetVisualColor(biome, height, normal, seed, terrainSettings, palette)
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
            WorldTerrainGenerationSettings terrainSettings,
            TerrainVisualPalette palette)
        {
            var color = GetBiomeColor(biome, palette);
            color = ApplyBiomeTextureColor(color, biome, normal, seed, palette);
            color = ApplyBiomeWaterTint(color, biome, height, terrainSettings, palette);

            var reliefShade = Mathf.Lerp(0.985f, 1.025f, height);
            var fineNoise = Noise3(normal, seed, 74f, 71.1f);
            var textureShade = Mathf.Lerp(0.975f, 1.025f, fineNoise);

            if (biome == BiomeType.Ocean)
            {
                textureShade = Mathf.Lerp(0.985f, 1.018f, fineNoise);
                reliefShade = 1f;
            }
            else if (biome == BiomeType.Forest || biome == BiomeType.DeadForest)
            {
                color = Color.Lerp(color, palette.ForestTint, 0.10f + fineNoise * 0.08f);
                textureShade = Mathf.Lerp(0.96f, 1.035f, fineNoise);
            }
            else if (biome == BiomeType.Mountains)
            {
                color = Color.Lerp(color, palette.VisualMountainTint, Mathf.Clamp01((height - 0.62f) * 1.6f));
                textureShade = Mathf.Lerp(0.94f, 1.055f, fineNoise);
            }
            else if (biome == BiomeType.Desert || biome == BiomeType.RustDesert)
            {
                textureShade = Mathf.Lerp(0.97f, 1.035f, fineNoise);
            }

            color.r = Mathf.Clamp01(color.r * reliefShade * textureShade);
            color.g = Mathf.Clamp01(color.g * reliefShade * textureShade);
            color.b = Mathf.Clamp01(color.b * reliefShade * textureShade);
            color.a = 1f;
            return color;
        }

        private static Color ApplyBiomeWaterTint(
            Color color,
            BiomeType biome,
            float height,
            WorldTerrainGenerationSettings terrainSettings,
            TerrainVisualPalette palette)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                    var oceanDepth = Mathf.InverseLerp(terrainSettings.OceanThreshold, 0f, height);
                    return Color.Lerp(color, palette.DeepWaterTint, Mathf.Lerp(0.38f, 0.72f, oceanDepth));
                case BiomeType.Coast:
                    var shallowMix = Mathf.InverseLerp(terrainSettings.OceanThreshold, terrainSettings.CoastThreshold, height);
                    var shallow = Color.Lerp(palette.ShallowWaterTint, palette.ShoreTint, shallowMix);
                    return Color.Lerp(color, shallow, 0.46f);
                default:
                    return color;
            }
        }

        private static Color ApplyBiomeTextureColor(
            Color baseColor,
            BiomeType biome,
            Vector3 normal,
            int seed,
            TerrainVisualPalette palette)
        {
            if (!palette.TryGetBiomeTexture(biome, out var sample))
            {
                return baseColor;
            }

            var poleFade = Mathf.SmoothStep(0.10f, 0.28f, new Vector2(normal.x, normal.z).magnitude);
            var blend = GetBiomeTextureBlend(biome) * poleFade;
            if (blend <= 0.001f)
            {
                return baseColor;
            }

            var textureColor = sample.Sample(GetSurfaceTextureUv(normal, biome, seed));
            var luminance = textureColor.r * 0.299f + textureColor.g * 0.587f + textureColor.b * 0.114f;
            var shade = Mathf.Lerp(0.94f, 1.08f, luminance);
            var detailedBase = new Color(
                Mathf.Clamp01(baseColor.r * shade),
                Mathf.Clamp01(baseColor.g * shade),
                Mathf.Clamp01(baseColor.b * shade),
                1f);
            var tintedTexture = Color.Lerp(baseColor, textureColor, GetBiomeTextureColorStrength(biome));
            var color = Color.Lerp(detailedBase, tintedTexture, blend);
            color.a = 1f;
            return color;
        }

        private static Vector2 GetSurfaceTextureUv(Vector3 normal, BiomeType biome, int seed)
        {
            var longitude = Mathf.Atan2(normal.z, normal.x) / (Mathf.PI * 2f) + 0.5f;
            var latitude = Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f)) / Mathf.PI + 0.5f;
            var scale = GetBiomeTextureScale(biome);
            var warpX = (Noise3(normal, seed, 5.7f, 503.9f) - 0.5f) * 0.08f;
            var warpY = (Noise3(normal, seed, 7.3f, 829.1f) - 0.5f) * 0.08f;
            return new Vector2(
                longitude * scale.x + seed * 0.0137f + warpX,
                latitude * scale.y + seed * 0.0211f + warpY);
        }

        private static Vector2 GetBiomeTextureScale(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                case BiomeType.Coast:
                    return new Vector2(28f, 14f);
                case BiomeType.Mountains:
                case BiomeType.Snow:
                    return new Vector2(38f, 19f);
                case BiomeType.Forest:
                case BiomeType.DeadForest:
                    return new Vector2(34f, 17f);
                default:
                    return new Vector2(30f, 15f);
            }
        }

        private static float GetBiomeTextureBlend(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                    return 0.42f;
                case BiomeType.Coast:
                    return 0.40f;
                case BiomeType.Forest:
                case BiomeType.DeadForest:
                case BiomeType.Mountains:
                    return 0.62f;
                case BiomeType.Snow:
                    return 0.36f;
                default:
                    return 0.52f;
            }
        }

        private static float GetBiomeTextureColorStrength(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean:
                    return 0.58f;
                case BiomeType.Coast:
                    return 0.54f;
                case BiomeType.Snow:
                    return 0.46f;
                case BiomeType.Mountains:
                    return 0.68f;
                case BiomeType.Forest:
                case BiomeType.DeadForest:
                    return 0.64f;
                default:
                    return 0.60f;
            }
        }

        private TerrainVisualPalette CreateTerrainVisualPalette()
        {
            var biomeColorCount = 0;
            var fallbackColors = RenderSettings.FallbackBiomeColors;
            for (var index = 0; index < fallbackColors.Count; index++)
            {
                biomeColorCount = Mathf.Max(biomeColorCount, (int)fallbackColors[index].BiomeType + 1);
            }

            biomeColorCount = Mathf.Max(biomeColorCount, 1);
            var colors = new Color[biomeColorCount];
            for (var index = 0; index < colors.Length; index++)
            {
                colors[index] = RenderSettings.MissingBiomeColor;
            }

            for (var index = 0; index < fallbackColors.Count; index++)
            {
                var entry = fallbackColors[index];
                var colorIndex = (int)entry.BiomeType;
                if (colorIndex >= 0 && colorIndex < colors.Length)
                {
                    colors[colorIndex] = entry.Color;
                }
            }

            return new TerrainVisualPalette(
                colors,
                BuildBiomeTextureSamples(biomeColorCount),
                RenderSettings.MissingBiomeColor,
                RenderSettings.DeepWaterTint,
                RenderSettings.ShallowWaterTint,
                RenderSettings.ShoreTint,
                RenderSettings.ForestTextureTint,
                RenderSettings.VisualMountainTint);
        }

        private BiomeTextureSample[] BuildBiomeTextureSamples(int biomeColorCount)
        {
            if (artAtlas == null || artAtlas.BiomeAtlasTexture == null || artAtlas.BiomeSprites == null)
            {
                return Array.Empty<BiomeTextureSample>();
            }

            Color32[] pixels;
            try
            {
                pixels = artAtlas.BiomeAtlasTexture.GetPixels32();
            }
            catch (UnityException)
            {
                return Array.Empty<BiomeTextureSample>();
            }

            var texture = artAtlas.BiomeAtlasTexture;
            var sampleCount = Mathf.Max(biomeColorCount, artAtlas.BiomeSprites.Length);
            var samples = new BiomeTextureSample[sampleCount];
            for (var index = 0; index < artAtlas.BiomeSprites.Length; index++)
            {
                var sprite = artAtlas.BiomeSprites[index];
                if (sprite == null || sprite.texture != texture)
                {
                    continue;
                }

                var rect = sprite.textureRect;
                samples[index] = new BiomeTextureSample(
                    pixels,
                    texture.width,
                    texture.height,
                    Mathf.RoundToInt(rect.xMin),
                    Mathf.RoundToInt(rect.yMin),
                    Mathf.RoundToInt(rect.width),
                    Mathf.RoundToInt(rect.height));
            }

            return samples;
        }

        private static Color GetBiomeColor(BiomeType biome, TerrainVisualPalette palette)
        {
            var colorIndex = (int)biome;
            return colorIndex >= 0 && colorIndex < palette.BiomeColors.Length
                ? palette.BiomeColors[colorIndex]
                : palette.MissingBiomeColor;
        }

        private Color GetFactionMarkerColor(int factionId)
        {
            if (factionId >= 0 && configDatabase != null && configDatabase.TryGetFaction(factionId, out var factionConfig))
            {
                return factionConfig.Color;
            }

            return RenderSettings.GetFactionMarkerColor(factionId);
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

        private static GlobalMapSettlementFeatureSpriteId GetSettlementFeatureSpriteId(
            SettlementData settlement,
            FactionData[] factions)
        {
            return IsCapitalCell(settlement.CellId, factions)
                ? GlobalMapSettlementFeatureSpriteId.Level5
                : GlobalMapSettlementFeatureSpriteId.Level2;
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

        private void EnsureRenderLayers()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                TryAutoWireRenderLayers();
            }
#endif

            if (generatedRoot == null)
            {
                throw new InvalidOperationException("ProceduralGlobalMapRenderer requires a generated root reference.");
            }

            if (renderSettings == null)
            {
                throw new InvalidOperationException("ProceduralGlobalMapRenderer requires a GlobalMapProceduralRenderSettings reference.");
            }

            ValidateLayer(starfieldLayer, nameof(starfieldLayer));
            ValidateLayer(terrainLayer, nameof(terrainLayer));
            ValidateLayer(riversLayer, nameof(riversLayer));
            ValidateLayer(roadsLayer, nameof(roadsLayer));
            ValidateLayer(markerIconsLayer, nameof(markerIconsLayer));
            ValidateLayer(settlementFeaturesLayer, nameof(settlementFeaturesLayer));
            ValidateLayer(activityFeaturesLayer, nameof(activityFeaturesLayer));
            ValidateLayer(selectionLayer, nameof(selectionLayer));
        }

        private static void ValidateLayer(GlobalMapMeshLayer layer, string fieldName)
        {
            if (layer == null)
            {
                throw new InvalidOperationException($"ProceduralGlobalMapRenderer requires {fieldName}.");
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            TryAutoWireRenderLayers();
        }

        private void OnValidate()
        {
            TryAutoWireRenderLayers();
        }

        private void TryAutoWireRenderLayers()
        {
            var changed = false;
            changed |= AssignIfMissing(ref generatedRoot, transform.Find(GeneratedRootName));
            if (generatedRoot != null)
            {
                changed |= AssignIfMissing(ref renderSettings, generatedRoot.GetComponent<GlobalMapProceduralRenderSettings>());
                changed |= AssignIfMissing(ref starfieldLayer, FindLayer(GeneratedRootName, "Starfield Mesh"));
                changed |= AssignIfMissing(ref terrainLayer, FindLayer(GeneratedRootName, "Terrain Mesh"));
                changed |= AssignIfMissing(ref riversLayer, FindLayer(GeneratedRootName, "Rivers Mesh"));
                changed |= AssignIfMissing(ref roadsLayer, FindLayer(GeneratedRootName, "Roads Mesh"));
                changed |= AssignIfMissing(ref markerIconsLayer, FindLayer(GeneratedRootName, MarkerIconsObjectName) ?? FindLayer(GeneratedRootName, LegacyMarkersObjectName));
                changed |= AssignIfMissing(ref settlementFeaturesLayer, FindLayer(GeneratedRootName, SettlementFeaturesObjectName));
                changed |= AssignIfMissing(ref activityFeaturesLayer, FindLayer(GeneratedRootName, ActivityFeaturesObjectName));
                changed |= AssignIfMissing(ref selectionLayer, FindLayer(GeneratedRootName, SelectionObjectName));
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private GlobalMapMeshLayer FindLayer(string rootName, string layerName)
        {
            var root = generatedRoot != null ? generatedRoot : transform.Find(rootName);
            var child = root != null ? root.Find(layerName) : null;
            return child != null ? child.GetComponent<GlobalMapMeshLayer>() : null;
        }

        private static bool AssignIfMissing<T>(ref T target, T value)
            where T : UnityEngine.Object
        {
            if (target != null || value == null)
            {
                return false;
            }

            target = value;
            return true;
        }
#endif

        private void EnsureMaterials()
        {
            var settings = RenderSettings;
            EnsureMaterialInstance(
                ref vertexColorMaterial,
                ref vertexColorMaterialTemplateSource,
                settings.VertexColorMaterialTemplate,
                null,
                nameof(settings.VertexColorMaterialTemplate));
            EnsureTexturedMaterial(
                ref iconMaterial,
                ref iconMaterialTemplateSource,
                settings.IconMaterialTemplate,
                artAtlas != null ? artAtlas.IconAtlasTexture : null,
                nameof(settings.IconMaterialTemplate));
            EnsureTexturedMaterial(
                ref settlementFeatureMaterial,
                ref settlementFeatureMaterialTemplateSource,
                settings.IconMaterialTemplate,
                artAtlas != null ? artAtlas.SettlementFeatureAtlasTexture : null,
                nameof(settings.IconMaterialTemplate));
            EnsureTexturedMaterial(
                ref activityFeatureMaterial,
                ref activityFeatureMaterialTemplateSource,
                settings.IconMaterialTemplate,
                artAtlas != null ? artAtlas.ActivityFeatureAtlasTexture : null,
                nameof(settings.IconMaterialTemplate));
        }

        private Material EnsureTerrainMaterial(Texture texture)
        {
            var settings = RenderSettings;
            EnsureMaterialInstance(
                ref terrainMaterial,
                ref terrainMaterialTemplateSource,
                settings.TerrainMaterialTemplate,
                texture,
                nameof(settings.TerrainMaterialTemplate));
            return terrainMaterial;
        }

        private static void EnsureMaterialInstance(
            ref Material material,
            ref Material templateSource,
            Material template,
            Texture texture,
            string templateName)
        {
            if (template == null)
            {
                DestroyUnityObject(material);
                material = null;
                templateSource = null;
                throw new InvalidOperationException($"GlobalMapProceduralRenderSettings requires {templateName}.");
            }

            if (material != null && templateSource == template && MaterialTextureMatches(material, texture))
            {
                return;
            }

            DestroyUnityObject(material);
            material = CreateRuntimeMaterial(template, texture);
            templateSource = template;
        }

        private static void EnsureTexturedMaterial(
            ref Material material,
            ref Material templateSource,
            Material template,
            Texture texture,
            string templateName)
        {
            if (texture == null)
            {
                DestroyUnityObject(material);
                material = null;
                templateSource = null;
                return;
            }

            EnsureMaterialInstance(ref material, ref templateSource, template, texture, templateName);
        }

        private static Material CreateRuntimeMaterial(Material template, Texture texture)
        {
            var material = new Material(template)
            {
                hideFlags = GetGeneratedHideFlags()
            };
            if (texture != null)
            {
                ApplyMaterialTexture(material, texture);
            }

            SetEditorDirty(material);
            return material;
        }

        private static bool MaterialTextureMatches(Material material, Texture texture)
        {
            if (texture == null)
            {
                return true;
            }

            return (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == texture) ||
                   (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") == texture);
        }

        private static void ApplyMaterialTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
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

        private void SetGeneratedMesh(
            GlobalMapMeshLayer layer,
            string meshName,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<int> triangles,
            Material material)
        {
            if (vertices.Count == 0 || triangles.Count == 0)
            {
                ClearLayer(layer);
                return;
            }

            var mesh = CreateMesh(meshName, vertices, colors, triangles);
            if (normals.Count == vertices.Count)
            {
                mesh.SetNormals(normals);
            }

            mesh.RecalculateBounds();
            SetGeneratedMesh(layer, mesh, material);
        }

        private void SetGeneratedTexturedMesh(
            GlobalMapMeshLayer layer,
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
                ClearLayer(layer);
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
            SetGeneratedMesh(layer, mesh, material);
        }

        private static void SetGeneratedMesh(GlobalMapMeshLayer layer, Mesh mesh, Material material)
        {
            if (layer == null)
            {
                DestroyUnityObject(mesh);
                throw new InvalidOperationException("ProceduralGlobalMapRenderer has an unassigned mesh layer.");
            }

            layer.SetMesh(mesh, material);
        }

        private static void ClearLayer(GlobalMapMeshLayer layer)
        {
            if (layer != null)
            {
                layer.Clear();
            }
        }

        private void ApplyMapPointLayerVisibility()
        {
            if (markerIconsLayer != null)
            {
                markerIconsLayer.SetVisible(markerIconsVisible && markerIconsLayer.HasMesh);
            }

            if (settlementFeaturesLayer != null)
            {
                settlementFeaturesLayer.SetVisible(featureTexturesVisible && settlementFeaturesLayer.HasMesh);
            }

            if (activityFeaturesLayer != null)
            {
                activityFeaturesLayer.SetVisible(featureTexturesVisible && activityFeaturesLayer.HasMesh);
            }
        }

        private static HideFlags GetGeneratedHideFlags()
        {
            return RuntimeGeneratedHideFlags;
        }

        private static void SetEditorDirty(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && target != null && !HasDontSaveFlag(target))
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }

        private static bool HasDontSaveFlag(UnityEngine.Object target)
        {
            return target != null && (target.hideFlags & RuntimeGeneratedHideFlags) != 0;
        }

        private static Vector3 ToVector(WorldSpherePoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }

        private static Vector3 SlerpNormal(Vector3 from, Vector3 to, float t)
        {
            var clampedT = Mathf.Clamp01(t);
            return SlerpNormalUnclamped(from, to, clampedT);
        }

        private static Vector3 SlerpNormalUnclamped(Vector3 from, Vector3 to, float t)
        {
            var dot = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            if (dot > 0.9995f)
            {
                return Vector3.LerpUnclamped(from, to, t).normalized;
            }

            var theta = Mathf.Acos(dot);
            var sinTheta = Mathf.Sin(theta);
            if (sinTheta <= 0.000001f)
            {
                return Vector3.LerpUnclamped(from, to, t).normalized;
            }

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

        private static void DestroyUnityObject(UnityEngine.Object target)
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
                DestroyUnityObject(vertexColorMaterial);
                DestroyUnityObject(terrainMaterial);
                DestroyUnityObject(terrainSurfaceTexture);
                DestroyUnityObject(iconMaterial);
                DestroyUnityObject(settlementFeatureMaterial);
                DestroyUnityObject(activityFeatureMaterial);
                vertexColorMaterialTemplateSource = null;
                terrainMaterialTemplateSource = null;
                terrainSurfaceTexture = null;
                iconMaterialTemplateSource = null;
                settlementFeatureMaterialTemplateSource = null;
                activityFeatureMaterialTemplateSource = null;
            }
        }

        private struct SurfaceSample
        {
            public BiomeType Biome;
            public float Height;
            public Color Color;
        }

        private readonly struct TerrainVisualPalette
        {
            public readonly Color[] BiomeColors;
            public readonly BiomeTextureSample[] BiomeTextures;
            public readonly Color MissingBiomeColor;
            public readonly Color DeepWaterTint;
            public readonly Color ShallowWaterTint;
            public readonly Color ShoreTint;
            public readonly Color ForestTint;
            public readonly Color VisualMountainTint;

            public TerrainVisualPalette(
                Color[] biomeColors,
                BiomeTextureSample[] biomeTextures,
                Color missingBiomeColor,
                Color deepWaterTint,
                Color shallowWaterTint,
                Color shoreTint,
                Color forestTint,
                Color visualMountainTint)
            {
                BiomeColors = biomeColors ?? Array.Empty<Color>();
                BiomeTextures = biomeTextures ?? Array.Empty<BiomeTextureSample>();
                MissingBiomeColor = missingBiomeColor;
                DeepWaterTint = deepWaterTint;
                ShallowWaterTint = shallowWaterTint;
                ShoreTint = shoreTint;
                ForestTint = forestTint;
                VisualMountainTint = visualMountainTint;
            }

            public bool TryGetBiomeTexture(BiomeType biome, out BiomeTextureSample sample)
            {
                var index = (int)biome;
                if (index >= 0 && index < BiomeTextures.Length && BiomeTextures[index].IsValid)
                {
                    sample = BiomeTextures[index];
                    return true;
                }

                sample = default;
                return false;
            }
        }

        private readonly struct BiomeTextureSample
        {
            private readonly Color32[] pixels;
            private readonly int textureWidth;
            private readonly int textureHeight;
            private readonly int x;
            private readonly int y;
            private readonly int width;
            private readonly int height;

            public BiomeTextureSample(
                Color32[] pixels,
                int textureWidth,
                int textureHeight,
                int x,
                int y,
                int width,
                int height)
            {
                this.pixels = pixels;
                this.textureWidth = textureWidth;
                this.textureHeight = textureHeight;
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
            }

            public bool IsValid =>
                pixels != null &&
                textureWidth > 0 &&
                textureHeight > 0 &&
                width > 0 &&
                height > 0;

            public Color Sample(Vector2 uv)
            {
                if (!IsValid)
                {
                    return Color.white;
                }

                var localX = Mathf.FloorToInt(Mathf.Repeat(uv.x, 1f) * width);
                var localY = Mathf.FloorToInt(Mathf.Repeat(uv.y, 1f) * height);
                var pixelX = Mathf.Clamp(x + localX, 0, textureWidth - 1);
                var pixelY = Mathf.Clamp(y + localY, 0, textureHeight - 1);
                return pixels[pixelY * textureWidth + pixelX];
            }
        }

        private sealed class DirectionCellLookup
        {
            private readonly WorldCell[] cells;
            private readonly Vector3[] cellNormals;
            private readonly List<int>[] bins;
            private readonly int longitudeBins;
            private readonly int latitudeBins;
            private readonly int[] candidateCellIds = new int[SurfaceBlendCandidateCount];
            private readonly float[] candidateDots = new float[SurfaceBlendCandidateCount];

            public DirectionCellLookup(WorldCell[] cells, int longitudeSegments, int latitudeSegments)
            {
                this.cells = cells ?? Array.Empty<WorldCell>();
                longitudeBins = Mathf.Max(16, longitudeSegments);
                latitudeBins = Mathf.Max(8, latitudeSegments);
                cellNormals = new Vector3[this.cells.Length];
                bins = new List<int>[longitudeBins * latitudeBins];

                for (var cellIndex = 0; cellIndex < this.cells.Length; cellIndex++)
                {
                    var normal = ToVector(this.cells[cellIndex].SpherePosition).normalized;
                    cellNormals[cellIndex] = normal;
                    GetBin(normal, out var binX, out var binY);
                    var binIndex = GetBinIndex(binX, binY);
                    bins[binIndex] ??= new List<int>(8);
                    bins[binIndex].Add(cellIndex);
                }
            }

            public WorldCell GetCandidateCell(int index)
            {
                return cells[candidateCellIds[index]];
            }

            public float GetCandidateDot(int index)
            {
                return candidateDots[index];
            }

            public int FindNearest(Vector3 normal)
            {
                var candidateCount = 0;
                for (var index = 0; index < candidateCellIds.Length; index++)
                {
                    candidateCellIds[index] = WorldIds.None;
                    candidateDots[index] = -2f;
                }

                GetBin(normal, out var centerX, out var centerY);
                for (var radius = 0; radius <= SurfaceBlendMaxBinSearchRadius; radius++)
                {
                    for (var y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (y < 0 || y >= latitudeBins)
                        {
                            continue;
                        }

                        for (var x = centerX - radius; x <= centerX + radius; x++)
                        {
                            if (radius > 0 && Mathf.Abs(x - centerX) != radius && Mathf.Abs(y - centerY) != radius)
                            {
                                continue;
                            }

                            candidateCount = AddBinCandidates(WrapLongitudeBin(x), y, normal, candidateCount);
                        }
                    }

                    if (candidateCount >= SurfaceBlendCandidateCount && radius >= 1)
                    {
                        break;
                    }
                }

                if (candidateCount > 0)
                {
                    return candidateCount;
                }

                for (var cellIndex = 0; cellIndex < cellNormals.Length; cellIndex++)
                {
                    candidateCount = AddCandidate(cellIndex, Vector3.Dot(normal, cellNormals[cellIndex]), candidateCount);
                }

                return candidateCount;
            }

            private int AddBinCandidates(int binX, int binY, Vector3 normal, int candidateCount)
            {
                var bin = bins[GetBinIndex(binX, binY)];
                if (bin == null)
                {
                    return candidateCount;
                }

                for (var index = 0; index < bin.Count; index++)
                {
                    var cellIndex = bin[index];
                    candidateCount = AddCandidate(cellIndex, Vector3.Dot(normal, cellNormals[cellIndex]), candidateCount);
                }

                return candidateCount;
            }

            private int AddCandidate(int cellIndex, float dot, int candidateCount)
            {
                if (candidateCount >= SurfaceBlendCandidateCount && dot <= candidateDots[SurfaceBlendCandidateCount - 1])
                {
                    return candidateCount;
                }

                var insertIndex = candidateCount;
                if (insertIndex >= SurfaceBlendCandidateCount)
                {
                    insertIndex = SurfaceBlendCandidateCount - 1;
                }

                while (insertIndex > 0 && dot > candidateDots[insertIndex - 1])
                {
                    if (insertIndex < SurfaceBlendCandidateCount)
                    {
                        candidateDots[insertIndex] = candidateDots[insertIndex - 1];
                        candidateCellIds[insertIndex] = candidateCellIds[insertIndex - 1];
                    }

                    insertIndex--;
                }

                candidateDots[insertIndex] = dot;
                candidateCellIds[insertIndex] = cellIndex;
                return Mathf.Min(candidateCount + 1, SurfaceBlendCandidateCount);
            }

            private void GetBin(Vector3 normal, out int binX, out int binY)
            {
                var longitude = Mathf.Atan2(normal.z, normal.x);
                var latitude = Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f));
                var u = longitude / (Mathf.PI * 2f) + 0.5f;
                var v = latitude / Mathf.PI + 0.5f;
                binX = Mathf.Clamp(Mathf.FloorToInt(u * longitudeBins), 0, longitudeBins - 1);
                binY = Mathf.Clamp(Mathf.FloorToInt(v * latitudeBins), 0, latitudeBins - 1);
            }

            private int GetBinIndex(int binX, int binY)
            {
                return binY * longitudeBins + binX;
            }

            private int WrapLongitudeBin(int binX)
            {
                var wrapped = binX % longitudeBins;
                return wrapped < 0 ? wrapped + longitudeBins : wrapped;
            }
        }

    }
}
