using System;

namespace MercLord.Battle.Tiles
{
    [Serializable]
    public sealed class BattleTileMap
    {
        public BattleTileMap(int width, int height, BattleTile[] tiles)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (tiles.Length != width * height)
            {
                throw new ArgumentException("Tile array length must match width * height.", nameof(tiles));
            }

            Width = width;
            Height = height;
            Tiles = tiles;
        }

        public int Width { get; }
        public int Height { get; }
        public BattleTile[] Tiles { get; }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public int GetIndex(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException($"Tile coordinate outside map: {x}, {y}");
            }

            return y * Width + x;
        }

        public BattleTile GetTile(int x, int y)
        {
            return Tiles[GetIndex(x, y)];
        }
    }
}
