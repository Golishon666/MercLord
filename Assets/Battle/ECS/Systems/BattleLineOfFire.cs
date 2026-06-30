using System;
using MercLord.Battle.Tiles;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    internal static class BattleLineOfFire
    {
        public static bool HasClearLine(
            BattleTileMap tileMap,
            float2 start,
            float2 end,
            bool checkLineOfSight,
            bool checkProjectiles)
        {
            if (tileMap == null)
            {
                throw new ArgumentNullException(nameof(tileMap));
            }

            var x0 = (int)math.floor(start.x);
            var y0 = (int)math.floor(start.y);
            var x1 = (int)math.floor(end.x);
            var y1 = (int)math.floor(end.y);
            if (!tileMap.IsInside(x0, y0) || !tileMap.IsInside(x1, y1))
            {
                return false;
            }

            var dx = math.abs(x1 - x0);
            var dy = math.abs(y1 - y0);
            var stepX = x0 < x1 ? 1 : -1;
            var stepY = y0 < y1 ? 1 : -1;
            var error = dx - dy;
            var x = x0;
            var y = y0;

            while (true)
            {
                if ((x != x0 || y != y0) && Blocks(tileMap.GetTile(x, y), checkLineOfSight, checkProjectiles))
                {
                    return false;
                }

                if (x == x1 && y == y1)
                {
                    return true;
                }

                var doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y += stepY;
                }
            }
        }

        private static bool Blocks(BattleTile tile, bool checkLineOfSight, bool checkProjectiles)
        {
            return (checkLineOfSight && tile.BlocksLineOfSight) ||
                   (checkProjectiles && tile.BlocksProjectiles);
        }
    }
}
