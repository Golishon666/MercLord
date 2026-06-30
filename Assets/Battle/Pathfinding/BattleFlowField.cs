using System;
using MercLord.Battle.Tiles;
using Unity.Mathematics;

namespace MercLord.Battle.Pathfinding
{
    [Serializable]
    public struct FlowCell
    {
        public const ushort BlockedCost = ushort.MaxValue;

        public ushort Cost;
        public sbyte DirX;
        public sbyte DirY;

        public bool IsReachable => Cost < BlockedCost;
    }

    public sealed class BattleFlowField
    {
        private readonly FlowCell[] cells;

        public BattleFlowField(
            int width,
            int height,
            FlowCell[] cells,
            int targetX,
            int targetY)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Length != width * height)
            {
                throw new ArgumentException("Flow cell array length must match width * height.", nameof(cells));
            }

            Width = width;
            Height = height;
            this.cells = cells;
            TargetX = targetX;
            TargetY = targetY;
        }

        public int Width { get; }
        public int Height { get; }
        public int TargetX { get; }
        public int TargetY { get; }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public int GetIndex(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException($"Flow field coordinate outside map: {x}, {y}");
            }

            return y * Width + x;
        }

        public FlowCell GetCell(int x, int y)
        {
            return cells[GetIndex(x, y)];
        }

        public bool TryGetDirection(int x, int y, out int2 direction)
        {
            direction = default;
            if (!IsInside(x, y))
            {
                return false;
            }

            var cell = GetCell(x, y);
            if (!cell.IsReachable)
            {
                return false;
            }

            direction = new int2(cell.DirX, cell.DirY);
            return true;
        }

        public bool TryGetDirection(float2 position, out float2 direction)
        {
            direction = default;
            if (!TryGetDirection(
                    (int)math.floor(position.x),
                    (int)math.floor(position.y),
                    out var cellDirection))
            {
                return false;
            }

            direction = math.normalizesafe(new float2(cellDirection.x, cellDirection.y));
            return true;
        }
    }
}
