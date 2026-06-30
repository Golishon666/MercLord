using System;
using System.Reflection;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MercLord.Editor.Tests
{
    public sealed class BattleTilemapViewTests
    {
        [Test]
        public void RenderFillsVisualLayersFromBattleModel()
        {
            var fixture = CreateFixture();
            try
            {
                var model = CreateModel();

                fixture.View.Render(model);

                Assert.IsNotNull(fixture.Ground.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Ground.GetTile(new Vector3Int(1, 0, 0)));
                Assert.IsNotNull(fixture.Road.GetTile(new Vector3Int(1, 0, 0)));
                Assert.IsNull(fixture.Road.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Obstacle.GetTile(new Vector3Int(2, 0, 0)));

                Assert.IsNotNull(fixture.Overlay.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Overlay.GetTile(new Vector3Int(1, 1, 0)));
                Assert.IsNotNull(fixture.Overlay.GetTile(new Vector3Int(2, 1, 0)));

                Assert.AreEqual(BattleDebugOverlayMode.Walkability, fixture.View.DebugOverlayMode);
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(2, 0, 0)));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void DebugOverlayModesRenderWalkabilityCoverAndHeight()
        {
            var fixture = CreateFixture();
            try
            {
                var model = CreateModel();
                fixture.View.Render(model);

                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(2, 0, 0)));

                fixture.View.SetDebugOverlayMode(BattleDebugOverlayMode.Cover, model);
                Assert.IsNull(fixture.Debug.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(1, 0, 0)));
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(2, 0, 0)));

                fixture.View.SetDebugOverlayMode(BattleDebugOverlayMode.Height, model);
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNotNull(fixture.Debug.GetTile(new Vector3Int(1, 0, 0)));

                fixture.View.SetDebugOverlayMode(BattleDebugOverlayMode.SpatialBuckets, model);
                Assert.IsNull(fixture.Debug.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNull(fixture.Debug.GetTile(new Vector3Int(1, 0, 0)));
                Assert.IsNull(fixture.Debug.GetTile(new Vector3Int(2, 0, 0)));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void ClearRemovesAllRenderedTiles()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.View.Render(CreateModel());

                fixture.View.Clear();

                Assert.IsNull(fixture.Ground.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNull(fixture.Road.GetTile(new Vector3Int(1, 0, 0)));
                Assert.IsNull(fixture.Obstacle.GetTile(new Vector3Int(2, 0, 0)));
                Assert.IsNull(fixture.Overlay.GetTile(new Vector3Int(0, 0, 0)));
                Assert.IsNull(fixture.Debug.GetTile(new Vector3Int(2, 0, 0)));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static BattleModel CreateModel()
        {
            return new BattleModel
            {
                Width = 3,
                Height = 2,
                Tiles = new[]
                {
                    CreateTile(BattleTileSurface.Ground, true, CoverType.None, 0),
                    CreateTile(BattleTileSurface.Road, true, CoverType.Medium, 2),
                    CreateTile(BattleTileSurface.Obstacle, false, CoverType.Heavy, 4),
                    CreateTile(BattleTileSurface.Ground, true, CoverType.Light, 1),
                    CreateTile(BattleTileSurface.Ground, true, CoverType.None, 0),
                    CreateTile(BattleTileSurface.Obstacle, false, CoverType.Heavy, 4)
                },
                SpawnZones = new[]
                {
                    new BattleSpawnZone
                    {
                        Side = BattleSpawnSide.Attacker,
                        Area = new RectInt(0, 0, 1, 2),
                        ForwardDirection = Vector2Int.right
                    },
                    new BattleSpawnZone
                    {
                        Side = BattleSpawnSide.Defender,
                        Area = new RectInt(2, 0, 1, 2),
                        ForwardDirection = Vector2Int.left
                    }
                },
                Objectives = new[]
                {
                    new BattleObjectiveZone
                    {
                        Type = BattleObjectiveType.ControlPoint,
                        Area = new RectInt(1, 1, 1, 1),
                        Priority = 1
                    }
                }
            };
        }

        private static BattleTile CreateTile(
            BattleTileSurface surface,
            bool walkable,
            CoverType cover,
            sbyte height)
        {
            return new BattleTile
            {
                Walkable = walkable,
                Surface = surface,
                MoveCost = 1,
                Cover = cover,
                Height = height,
                AllowedMoveLayers = walkable ? MoveLayer.Infantry | MoveLayer.Vehicle : MoveLayer.None,
                BlocksLineOfSight = cover == CoverType.Heavy,
                BlocksProjectiles = cover == CoverType.Heavy,
                RegionId = 0
            };
        }

        private static TilemapFixture CreateFixture()
        {
            var root = new GameObject("Battle Tilemap View Test");
            root.AddComponent<Grid>();
            var view = root.AddComponent<BattleTilemapView>();
            var fixture = new TilemapFixture
            {
                Root = root,
                View = view,
                Ground = CreateTilemap(root.transform, "GroundTilemap"),
                Road = CreateTilemap(root.transform, "RoadTilemap"),
                Obstacle = CreateTilemap(root.transform, "ObstacleTilemap"),
                Decoration = CreateTilemap(root.transform, "DecorationTilemap"),
                Overlay = CreateTilemap(root.transform, "OverlayTilemap"),
                Debug = CreateTilemap(root.transform, "DebugTilemap")
            };

            SetField(view, "groundTilemap", fixture.Ground);
            SetField(view, "roadTilemap", fixture.Road);
            SetField(view, "obstacleTilemap", fixture.Obstacle);
            SetField(view, "decorationTilemap", fixture.Decoration);
            SetField(view, "overlayTilemap", fixture.Overlay);
            SetField(view, "debugTilemap", fixture.Debug);
            return fixture;
        }

        private static Tilemap CreateTilemap(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var tilemap = gameObject.AddComponent<Tilemap>();
            gameObject.AddComponent<TilemapRenderer>();
            return tilemap;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
            }

            field.SetValue(target, value);
        }

        private sealed class TilemapFixture : IDisposable
        {
            public GameObject Root;
            public BattleTilemapView View;
            public Tilemap Ground;
            public Tilemap Road;
            public Tilemap Obstacle;
            public Tilemap Decoration;
            public Tilemap Overlay;
            public Tilemap Debug;

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }
        }
    }
}
