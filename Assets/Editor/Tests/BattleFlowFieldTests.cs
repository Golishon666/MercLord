using System;
using MercLord.Battle.Pathfinding;
using MercLord.Battle.Tiles;
using NUnit.Framework;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattleFlowFieldTests
    {
        [Test]
        public void BuildRoutesAroundBlockedTiles()
        {
            var map = CreateMap(5, 5);
            Block(map, 1, 1);
            Block(map, 1, 2);
            Block(map, 1, 3);

            var field = new BattleFlowFieldBuilder().Build(map, 4, 2, MoveLayer.Infantry);

            Assert.IsFalse(field.GetCell(1, 2).IsReachable);
            Assert.IsTrue(field.GetCell(0, 2).IsReachable);
            Assert.IsTrue(field.TryGetDirection(0, 2, out var direction));
            Assert.AreEqual(0, direction.x);
            Assert.AreEqual(1, math.abs(direction.y));
        }

        [Test]
        public void BuildPrefersCheaperRoadOverShortExpensiveGround()
        {
            var map = CreateMap(5, 3, defaultMoveCost: 8);
            for (var x = 0; x < map.Width; x++)
            {
                SetTile(map, x, 0, CreateTile(moveCost: 1, surface: BattleTileSurface.Road));
                SetTile(map, x, 1, CreateTile(moveCost: 8));
                SetTile(map, x, 2, CreateTile(moveCost: 8));
            }

            var field = new BattleFlowFieldBuilder().Build(map, 4, 1, MoveLayer.Infantry);

            Assert.IsTrue(field.TryGetDirection(0, 1, out var direction));
            Assert.AreEqual(-1, direction.y);
        }

        [Test]
        public void BuildRejectsBlockedTarget()
        {
            var map = CreateMap(3, 3);
            Block(map, 1, 1);

            Assert.Throws<InvalidOperationException>(
                () => new BattleFlowFieldBuilder().Build(map, 1, 1, MoveLayer.Infantry));
        }

        [Test]
        public void TryGetDirectionSupportsFloatPositionsAndRejectsUnreachableCells()
        {
            var map = CreateMap(3, 3);
            Block(map, 1, 1);
            var field = new BattleFlowFieldBuilder().Build(map, 2, 2, MoveLayer.Infantry);

            Assert.IsTrue(field.TryGetDirection(new float2(0.25f, 0.25f), out var direction));
            Assert.Greater(math.lengthsq(direction), 0f);

            Assert.IsFalse(field.TryGetDirection(1, 1, out _));
            Assert.IsFalse(field.TryGetDirection(new float2(-1f, 0f), out _));
        }

        private static BattleTileMap CreateMap(
            int width,
            int height,
            byte defaultMoveCost = 1)
        {
            var tiles = new BattleTile[width * height];
            for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
            {
                tiles[tileIndex] = CreateTile(defaultMoveCost);
            }

            return new BattleTileMap(width, height, tiles);
        }

        private static BattleTile CreateTile(
            byte moveCost = 1,
            BattleTileSurface surface = BattleTileSurface.Ground)
        {
            return new BattleTile
            {
                Walkable = true,
                Surface = surface,
                MoveCost = moveCost,
                AllowedMoveLayers = MoveLayer.Infantry | MoveLayer.Vehicle
            };
        }

        private static void Block(BattleTileMap map, int x, int y)
        {
            SetTile(
                map,
                x,
                y,
                new BattleTile
                {
                    Walkable = false,
                    Surface = BattleTileSurface.Obstacle,
                    MoveCost = 1,
                    AllowedMoveLayers = MoveLayer.None,
                    BlocksLineOfSight = true,
                    BlocksProjectiles = true
                });
        }

        private static void SetTile(BattleTileMap map, int x, int y, BattleTile tile)
        {
            map.Tiles[map.GetIndex(x, y)] = tile;
        }
    }
}
