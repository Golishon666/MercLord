using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    [Serializable]
    public struct BiomeColorEntry
    {
        [SerializeField] private BiomeType biomeType;
        [SerializeField] private Color color;

        public BiomeColorEntry(BiomeType biomeType, Color color)
        {
            this.biomeType = biomeType;
            this.color = color;
        }

        public BiomeType BiomeType => biomeType;
        public Color Color => color;
    }

    public sealed class GlobalMapProceduralRenderSettings : MonoBehaviour
    {
        [Header("World Scale")]
        [SerializeField, Min(0.1f)] private float planetRadius = 3f;
        [SerializeField, Min(0.1f)] private float starfieldRadius = 18f;

        [Header("Mesh Detail")]
        [SerializeField, Min(1)] private int lineSegments = 8;
        [SerializeField, Min(1)] private int starCount = 720;
        [SerializeField, Min(3)] private int biomeUnderlayLongitudeSegments = 192;
        [SerializeField, Min(2)] private int biomeUnderlayLatitudeSegments = 96;
        [SerializeField, Range(12, 32)] private int tileVoronoiSeedVertexCount = 12;
        [SerializeField, Min(1f)] private float tileVoronoiSeedRadiusMultiplier = 2.4f;

        [Header("Surface Offsets")]
        [SerializeField] private float terrainSurfaceOffset = 0.006f;
        [SerializeField] private float biomeUnderlayOffset = -0.045f;
        [SerializeField] private float selectionSurfaceOffset = 0.032f;
        [SerializeField] private float riverSurfaceOffset = 0.014f;
        [SerializeField] private float roadSurfaceOffset = 0.018f;
        [SerializeField] private float legacySettlementSurfaceOffset = 0.034f;
        [SerializeField] private float legacyActivitySurfaceOffset = 0.032f;
        [SerializeField] private float settlementIconSurfaceOffset = 0.04f;
        [SerializeField] private float activityIconSurfaceOffset = 0.038f;
        [SerializeField] private float settlementFeatureSurfaceOffset = 0.046f;
        [SerializeField] private float activityFeatureSurfaceOffset = 0.044f;
        [SerializeField] private float legacyFlatIconNudge = 0.003f;
        [SerializeField] private float spriteMarkerNudge = 0.004f;

        [Header("Starfield")]
        [SerializeField] private Vector2 starSizeRange = new(0.011f, 0.035f);
        [SerializeField] private Vector2 starBrightnessRange = new(0.55f, 1f);
        [SerializeField] private Color starTint = new(0.68f, 0.78f, 1f, 1f);

        [Header("Terrain Texture")]
        [SerializeField, Range(0f, 0.45f)] private float biomeTileUvPadding = 0.025f;

        [Header("Materials")]
        [SerializeField] private Material vertexColorMaterialTemplate;
        [SerializeField] private Material biomeMaterialTemplate;
        [SerializeField] private Material iconMaterialTemplate;

        [Header("Terrain Fallback Colors")]
        [SerializeField] private Color missingBiomeColor = Color.magenta;
        [SerializeField] private Color forestTextureTint = new(0.14f, 0.25f, 0.09f, 1f);
        [SerializeField] private Color mountainTextureTint = new(0.70f, 0.68f, 0.62f, 1f);
        [SerializeField] private Color visualMountainTint = new(0.72f, 0.70f, 0.66f, 1f);
        [SerializeField] private Color oceanTextureTint = new(0.07f, 0.16f, 0.28f, 1f);
        [SerializeField] private Color deepWaterTint = new(0.06f, 0.14f, 0.26f, 1f);
        [SerializeField] private Color shallowWaterTint = new(0.16f, 0.31f, 0.44f, 1f);
        [SerializeField] private Color shoreTint = new(0.48f, 0.45f, 0.30f, 1f);
        [SerializeField] private BiomeColorEntry[] fallbackBiomeColors =
        {
            new(BiomeType.Ocean, new Color(0.08f, 0.18f, 0.31f, 1f)),
            new(BiomeType.Coast, new Color(0.43f, 0.43f, 0.24f, 1f)),
            new(BiomeType.Plains, new Color(0.42f, 0.41f, 0.20f, 1f)),
            new(BiomeType.Forest, new Color(0.20f, 0.31f, 0.14f, 1f)),
            new(BiomeType.Desert, new Color(0.58f, 0.44f, 0.31f, 1f)),
            new(BiomeType.Snow, new Color(0.78f, 0.80f, 0.76f, 1f)),
            new(BiomeType.Swamp, new Color(0.28f, 0.34f, 0.22f, 1f)),
            new(BiomeType.Mountains, new Color(0.46f, 0.42f, 0.36f, 1f)),
            new(BiomeType.AshWastes, new Color(0.25f, 0.23f, 0.22f, 1f)),
            new(BiomeType.RustDesert, new Color(0.55f, 0.35f, 0.25f, 1f)),
            new(BiomeType.DeadForest, new Color(0.29f, 0.32f, 0.22f, 1f)),
            new(BiomeType.IndustrialRuins, new Color(0.34f, 0.35f, 0.33f, 1f)),
            new(BiomeType.DemonScar, new Color(0.43f, 0.12f, 0.12f, 1f)),
            new(BiomeType.ToxicSwamp, new Color(0.31f, 0.42f, 0.16f, 1f)),
        };

        [Header("Roads and Rivers")]
        [SerializeField] private float riverBaseWidth = 0.0045f;
        [SerializeField] private float riverFlowWidthMultiplier = 0.0018f;
        [SerializeField] private float riverMaxFlowWidth = 6f;
        [SerializeField] private Color riverColor = new(0.08f, 0.22f, 0.38f, 0.9f);
        [SerializeField] private float largeRoadWidth = 0.011f;
        [SerializeField] private float mediumRoadWidth = 0.0075f;
        [SerializeField] private float smallRoadWidth = 0.0045f;
        [SerializeField] private Color largeRoadColor = new(0.16f, 0.15f, 0.13f, 0.95f);
        [SerializeField] private Color mediumRoadColor = new(0.23f, 0.20f, 0.16f, 0.88f);
        [SerializeField] private Color smallRoadColor = new(0.18f, 0.17f, 0.15f, 0.74f);

        [Header("Markers")]
        [SerializeField] private float capitalMarkerIconSize = 0.085f;
        [SerializeField] private float settlementMarkerIconSize = 0.0625f;
        [SerializeField] private float activityMarkerIconSize = 0.065f;
        [SerializeField] private float legacyCapitalMarkerSize = 0.085f;
        [SerializeField] private float legacySettlementMarkerSize = 0.0575f;
        [SerializeField] private float legacyActivityMarkerSize = 0.06f;
        [SerializeField] private float legacyFlatIconOutlineScale = 1.14f;
        [SerializeField] private Color unknownFactionMarkerColor = new(0.68f, 0.72f, 0.78f, 1f);
        [SerializeField] private Color[] factionMarkerColors =
        {
            new(0.12f, 0.50f, 0.95f, 1f),
            new(0.45f, 0.34f, 0.86f, 1f),
            new(0.66f, 0.84f, 1.00f, 1f),
            new(0.30f, 0.78f, 0.44f, 1f),
            new(0.95f, 0.38f, 0.35f, 1f),
            new(0.95f, 0.70f, 0.78f, 1f)
        };
        [SerializeField] private Vector2[] legacySettlementShape =
        {
            new(-0.44f, -0.48f),
            new(0.44f, -0.48f),
            new(0.44f, 0.08f),
            new(0.00f, 0.56f),
            new(-0.44f, 0.08f)
        };
        [SerializeField] private Vector2[] legacyActivityShape =
        {
            new(-0.48f, -0.42f),
            new(0.48f, -0.42f),
            new(0.00f, 0.54f)
        };
        [SerializeField] private Vector2[] legacyCaravanStopShape =
        {
            new(0.00f, -0.52f),
            new(0.48f, 0.00f),
            new(0.00f, 0.52f),
            new(-0.48f, 0.00f)
        };

        [Header("Selection")]
        [SerializeField] private Color selectionColor = new(0.96f, 0.98f, 0.68f, 1f);

        public float PlanetRadius => Mathf.Max(0.1f, planetRadius);
        public float StarfieldRadius => Mathf.Max(0.1f, starfieldRadius);
        public int LineSegments => Mathf.Max(1, lineSegments);
        public int StarCount => Mathf.Max(1, starCount);
        public int BiomeUnderlayLongitudeSegments => Mathf.Max(3, biomeUnderlayLongitudeSegments);
        public int BiomeUnderlayLatitudeSegments => Mathf.Max(2, biomeUnderlayLatitudeSegments);
        public int TileVoronoiSeedVertexCount => Mathf.Max(12, tileVoronoiSeedVertexCount);
        public float TileVoronoiSeedRadiusMultiplier => Mathf.Max(1f, tileVoronoiSeedRadiusMultiplier);
        public float TerrainSurfaceOffset => terrainSurfaceOffset;
        public float BiomeUnderlayOffset => biomeUnderlayOffset;
        public float SelectionSurfaceOffset => selectionSurfaceOffset;
        public float RiverSurfaceOffset => riverSurfaceOffset;
        public float RoadSurfaceOffset => roadSurfaceOffset;
        public float LegacySettlementSurfaceOffset => legacySettlementSurfaceOffset;
        public float LegacyActivitySurfaceOffset => legacyActivitySurfaceOffset;
        public float SettlementIconSurfaceOffset => settlementIconSurfaceOffset;
        public float ActivityIconSurfaceOffset => activityIconSurfaceOffset;
        public float SettlementFeatureSurfaceOffset => settlementFeatureSurfaceOffset;
        public float ActivityFeatureSurfaceOffset => activityFeatureSurfaceOffset;
        public float LegacyFlatIconNudge => legacyFlatIconNudge;
        public float SpriteMarkerNudge => spriteMarkerNudge;
        public Vector2 StarSizeRange => starSizeRange;
        public Vector2 StarBrightnessRange => starBrightnessRange;
        public Color StarTint => starTint;
        public float BiomeTileUvPadding => biomeTileUvPadding;
        public Material VertexColorMaterialTemplate => vertexColorMaterialTemplate;
        public Material BiomeMaterialTemplate => biomeMaterialTemplate;
        public Material IconMaterialTemplate => iconMaterialTemplate;
        public Color ForestTextureTint => forestTextureTint;
        public Color MountainTextureTint => mountainTextureTint;
        public Color VisualMountainTint => visualMountainTint;
        public Color OceanTextureTint => oceanTextureTint;
        public Color DeepWaterTint => deepWaterTint;
        public Color ShallowWaterTint => shallowWaterTint;
        public Color ShoreTint => shoreTint;
        public float RiverBaseWidth => riverBaseWidth;
        public float RiverFlowWidthMultiplier => riverFlowWidthMultiplier;
        public float RiverMaxFlowWidth => riverMaxFlowWidth;
        public Color RiverColor => riverColor;
        public float CapitalMarkerIconSize => capitalMarkerIconSize;
        public float SettlementMarkerIconSize => settlementMarkerIconSize;
        public float ActivityMarkerIconSize => activityMarkerIconSize;
        public float LegacyCapitalMarkerSize => legacyCapitalMarkerSize;
        public float LegacySettlementMarkerSize => legacySettlementMarkerSize;
        public float LegacyActivityMarkerSize => legacyActivityMarkerSize;
        public float LegacyFlatIconOutlineScale => legacyFlatIconOutlineScale;
        public Color SelectionColor => selectionColor;
        public IReadOnlyList<Vector2> LegacySettlementShape => legacySettlementShape ?? Array.Empty<Vector2>();
        public IReadOnlyList<Vector2> LegacyActivityShape => legacyActivityShape ?? Array.Empty<Vector2>();
        public IReadOnlyList<Vector2> LegacyCaravanStopShape => legacyCaravanStopShape ?? Array.Empty<Vector2>();
        public IReadOnlyList<BiomeColorEntry> FallbackBiomeColors => fallbackBiomeColors ?? Array.Empty<BiomeColorEntry>();

        public float GetRoadWidth(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Large:
                    return largeRoadWidth;
                case RoadType.Medium:
                    return mediumRoadWidth;
                default:
                    return smallRoadWidth;
            }
        }

        public Color GetRoadColor(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Large:
                    return largeRoadColor;
                case RoadType.Medium:
                    return mediumRoadColor;
                default:
                    return smallRoadColor;
            }
        }

        public Color GetFactionMarkerColor(int factionId)
        {
            if (factionId < 0 || factionMarkerColors == null || factionMarkerColors.Length == 0)
            {
                return unknownFactionMarkerColor;
            }

            return factionMarkerColors[factionId % factionMarkerColors.Length];
        }

        public Color GetFallbackBiomeColor(BiomeType biomeType)
        {
            if (fallbackBiomeColors == null)
            {
                return missingBiomeColor;
            }

            for (var entryIndex = 0; entryIndex < fallbackBiomeColors.Length; entryIndex++)
            {
                var entry = fallbackBiomeColors[entryIndex];
                if (entry.BiomeType == biomeType)
                {
                    return entry.Color;
                }
            }

            return missingBiomeColor;
        }
    }
}
