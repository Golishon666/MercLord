using System;
using System.Collections.Generic;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleTilemapView : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap roadTilemap;
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] private Tilemap decorationTilemap;
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private Tilemap debugTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase groundTile;
        [SerializeField] private TileBase roadTile;
        [SerializeField] private TileBase obstacleTile;
        [SerializeField] private TileBase attackerSpawnOverlayTile;
        [SerializeField] private TileBase defenderSpawnOverlayTile;
        [SerializeField] private TileBase objectiveOverlayTile;
        [SerializeField] private TileBase debugWalkableTile;
        [SerializeField] private TileBase debugBlockedTile;
        [SerializeField] private TileBase debugLightCoverTile;
        [SerializeField] private TileBase debugMediumCoverTile;
        [SerializeField] private TileBase debugHeavyCoverTile;
        [SerializeField] private TileBase debugLowHeightTile;
        [SerializeField] private TileBase debugHighHeightTile;

        [Header("Fallback Colors")]
        [SerializeField] private Color groundColor = new Color(0.29f, 0.35f, 0.24f, 1f);
        [SerializeField] private Color roadColor = new Color(0.38f, 0.34f, 0.27f, 1f);
        [SerializeField] private Color obstacleColor = new Color(0.17f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color attackerSpawnColor = new Color(0.18f, 0.48f, 0.95f, 0.35f);
        [SerializeField] private Color defenderSpawnColor = new Color(0.9f, 0.18f, 0.14f, 0.35f);
        [SerializeField] private Color objectiveColor = new Color(1f, 0.78f, 0.18f, 0.38f);
        [SerializeField] private Color debugWalkableColor = new Color(0.1f, 0.95f, 0.24f, 0.35f);
        [SerializeField] private Color debugBlockedColor = new Color(0.95f, 0.1f, 0.1f, 0.45f);
        [SerializeField] private Color debugLightCoverColor = new Color(0.96f, 0.9f, 0.25f, 0.35f);
        [SerializeField] private Color debugMediumCoverColor = new Color(0.96f, 0.55f, 0.18f, 0.42f);
        [SerializeField] private Color debugHeavyCoverColor = new Color(0.95f, 0.12f, 0.08f, 0.52f);
        [SerializeField] private Color debugLowHeightColor = new Color(0.15f, 0.38f, 0.95f, 0.35f);
        [SerializeField] private Color debugHighHeightColor = new Color(0.95f, 0.95f, 0.95f, 0.5f);
        [SerializeField] private BattleDebugOverlayMode debugOverlayMode = BattleDebugOverlayMode.Walkability;
        [SerializeField, Min(1)] private int debugHeightGradientMax = 8;

        private readonly Dictionary<sbyte, TileBase> heightDebugTiles = new Dictionary<sbyte, TileBase>();
        private readonly List<TileBase> runtimeTiles = new List<TileBase>();
        private bool fallbackTilesInitialized;

        public Tilemap GroundTilemap => groundTilemap;
        public Tilemap RoadTilemap => roadTilemap;
        public Tilemap ObstacleTilemap => obstacleTilemap;
        public Tilemap DecorationTilemap => decorationTilemap;
        public Tilemap OverlayTilemap => overlayTilemap;
        public Tilemap DebugTilemap => debugTilemap;
        public BattleDebugOverlayMode DebugOverlayMode => debugOverlayMode;

        public void Render(BattleModel model)
        {
            ValidateModel(model);
            ValidateReferences();
            EnsureFallbackTiles();

            Clear();

            var bounds = new BoundsInt(0, 0, 0, model.Width, model.Height, 1);
            var cellCount = model.Width * model.Height;
            var groundTiles = new TileBase[cellCount];
            var roadTiles = new TileBase[cellCount];
            var obstacleTiles = new TileBase[cellCount];
            var debugTiles = new TileBase[cellCount];

            for (var tileIndex = 0; tileIndex < model.Tiles.Length; tileIndex++)
            {
                var tile = model.Tiles[tileIndex];
                groundTiles[tileIndex] = groundTile;

                if (tile.Surface == BattleTileSurface.Road)
                {
                    roadTiles[tileIndex] = roadTile;
                }

                if (tile.Surface == BattleTileSurface.Obstacle || !tile.Walkable)
                {
                    obstacleTiles[tileIndex] = obstacleTile;
                }

                debugTiles[tileIndex] = ResolveDebugTile(tile);
            }

            groundTilemap.SetTilesBlock(bounds, groundTiles);
            roadTilemap.SetTilesBlock(bounds, roadTiles);
            obstacleTilemap.SetTilesBlock(bounds, obstacleTiles);
            debugTilemap.SetTilesBlock(bounds, debugTiles);
            RenderSpawnZones(model);
            RenderObjectiveZones(model);
        }

        public void Clear()
        {
            groundTilemap?.ClearAllTiles();
            roadTilemap?.ClearAllTiles();
            obstacleTilemap?.ClearAllTiles();
            decorationTilemap?.ClearAllTiles();
            overlayTilemap?.ClearAllTiles();
            debugTilemap?.ClearAllTiles();
        }

        public void SetDebugOverlayMode(BattleDebugOverlayMode mode, BattleModel model)
        {
            debugOverlayMode = mode;
            Render(model);
        }

        private void RenderSpawnZones(BattleModel model)
        {
            var spawnZones = model.SpawnZones ?? Array.Empty<BattleSpawnZone>();
            for (var zoneIndex = 0; zoneIndex < spawnZones.Length; zoneIndex++)
            {
                var zone = spawnZones[zoneIndex];
                var tile = zone.Side == BattleSpawnSide.Attacker
                    ? attackerSpawnOverlayTile
                    : defenderSpawnOverlayTile;
                var xMin = Mathf.Max(0, zone.Area.xMin);
                var xMax = Mathf.Min(model.Width, zone.Area.xMax);
                var yMin = Mathf.Max(0, zone.Area.yMin);
                var yMax = Mathf.Min(model.Height, zone.Area.yMax);

                for (var y = yMin; y < yMax; y++)
                {
                    for (var x = xMin; x < xMax; x++)
                    {
                        overlayTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
        }

        private void RenderObjectiveZones(BattleModel model)
        {
            var objectives = model.Objectives ?? Array.Empty<BattleObjectiveZone>();
            for (var objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                var objective = objectives[objectiveIndex];
                var xMin = Mathf.Max(0, objective.Area.xMin);
                var xMax = Mathf.Min(model.Width, objective.Area.xMax);
                var yMin = Mathf.Max(0, objective.Area.yMin);
                var yMax = Mathf.Min(model.Height, objective.Area.yMax);

                for (var y = yMin; y < yMax; y++)
                {
                    for (var x = xMin; x < xMax; x++)
                    {
                        overlayTilemap.SetTile(new Vector3Int(x, y, 0), objectiveOverlayTile);
                    }
                }
            }
        }

        private TileBase ResolveDebugTile(BattleTile tile)
        {
            switch (debugOverlayMode)
            {
                case BattleDebugOverlayMode.None:
                    return null;
                case BattleDebugOverlayMode.Walkability:
                    return tile.Walkable ? debugWalkableTile : debugBlockedTile;
                case BattleDebugOverlayMode.Cover:
                    return ResolveCoverDebugTile(tile.Cover);
                case BattleDebugOverlayMode.Height:
                    return ResolveHeightDebugTile(tile.Height);
                case BattleDebugOverlayMode.SpatialBuckets:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(debugOverlayMode));
            }
        }

        private TileBase ResolveCoverDebugTile(CoverType cover)
        {
            switch (cover)
            {
                case CoverType.None:
                    return null;
                case CoverType.Light:
                    return debugLightCoverTile;
                case CoverType.Medium:
                    return debugMediumCoverTile;
                case CoverType.Heavy:
                    return debugHeavyCoverTile;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cover));
            }
        }

        private TileBase ResolveHeightDebugTile(sbyte height)
        {
            if (height <= 0)
            {
                return debugLowHeightTile;
            }

            if (!heightDebugTiles.TryGetValue(height, out var tile))
            {
                var normalized = Mathf.Clamp01((float)height / Mathf.Max(1, debugHeightGradientMax));
                tile = CreateRuntimeTile(Color.Lerp(debugLowHeightColor, debugHighHeightColor, normalized));
                heightDebugTiles.Add(height, tile);
            }

            return tile;
        }

        private void EnsureFallbackTiles()
        {
            if (fallbackTilesInitialized)
            {
                return;
            }

            groundTile ??= CreateRuntimeTile(groundColor);
            roadTile ??= CreateRuntimeTile(roadColor);
            obstacleTile ??= CreateRuntimeTile(obstacleColor);
            attackerSpawnOverlayTile ??= CreateRuntimeTile(attackerSpawnColor);
            defenderSpawnOverlayTile ??= CreateRuntimeTile(defenderSpawnColor);
            objectiveOverlayTile ??= CreateRuntimeTile(objectiveColor);
            debugWalkableTile ??= CreateRuntimeTile(debugWalkableColor);
            debugBlockedTile ??= CreateRuntimeTile(debugBlockedColor);
            debugLightCoverTile ??= CreateRuntimeTile(debugLightCoverColor);
            debugMediumCoverTile ??= CreateRuntimeTile(debugMediumCoverColor);
            debugHeavyCoverTile ??= CreateRuntimeTile(debugHeavyCoverColor);
            debugLowHeightTile ??= CreateRuntimeTile(debugLowHeightColor);
            debugHighHeightTile ??= CreateRuntimeTile(debugHighHeightColor);
            fallbackTilesInitialized = true;
        }

        private static Tile CreateTile(Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private TileBase CreateRuntimeTile(Color color)
        {
            var tile = CreateTile(color);
            runtimeTiles.Add(tile);
            return tile;
        }

        private void OnDestroy()
        {
            for (var tileIndex = runtimeTiles.Count - 1; tileIndex >= 0; tileIndex--)
            {
                var tile = runtimeTiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(tile);
                }
                else
                {
                    DestroyImmediate(tile);
                }
            }

            runtimeTiles.Clear();
            heightDebugTiles.Clear();
        }

        private void ValidateReferences()
        {
            if (groundTilemap == null ||
                roadTilemap == null ||
                obstacleTilemap == null ||
                decorationTilemap == null ||
                overlayTilemap == null ||
                debugTilemap == null)
            {
                throw new InvalidOperationException("BattleTilemapView requires all Tilemap layer references.");
            }
        }

        private static void ValidateModel(BattleModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Width <= 0 || model.Height <= 0)
            {
                throw new InvalidOperationException("BattleModel dimensions must be positive.");
            }

            if (model.Tiles == null || model.Tiles.Length != model.Width * model.Height)
            {
                throw new InvalidOperationException("BattleModel tiles must match map dimensions.");
            }
        }
    }
}
