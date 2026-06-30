using System;
using System.Collections.Generic;
using MercLord.Battle.Tiles;
using Unity.Mathematics;

namespace MercLord.Battle.Pathfinding
{
    public sealed class BattleFlowFieldBuilder
    {
        private const uint UnvisitedCost = uint.MaxValue;
        private static readonly int2[] Directions =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1),
            new int2(1, 1),
            new int2(1, -1),
            new int2(-1, 1),
            new int2(-1, -1)
        };

        public BattleFlowField Build(
            BattleTileMap map,
            int targetX,
            int targetY,
            MoveLayer moveLayer)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (moveLayer == MoveLayer.None)
            {
                throw new ArgumentException("Flow field move layer must not be None.", nameof(moveLayer));
            }

            var targetIndex = map.GetIndex(targetX, targetY);
            if (!IsEnterable(map, targetX, targetY, moveLayer))
            {
                throw new InvalidOperationException("Flow field target tile must be enterable.");
            }

            var distances = new uint[map.Width * map.Height];
            for (var index = 0; index < distances.Length; index++)
            {
                distances[index] = UnvisitedCost;
            }

            var queue = new MinHeap();
            distances[targetIndex] = 0u;
            queue.Enqueue(targetIndex, 0u);

            while (queue.TryDequeue(out var currentIndex, out var currentCost))
            {
                if (currentCost != distances[currentIndex])
                {
                    continue;
                }

                var currentX = currentIndex % map.Width;
                var currentY = currentIndex / map.Width;
                for (var directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                {
                    var direction = Directions[directionIndex];
                    var nextX = currentX + direction.x;
                    var nextY = currentY + direction.y;
                    if (!CanTraverse(map, currentX, currentY, nextX, nextY, moveLayer))
                    {
                        continue;
                    }

                    var nextIndex = map.GetIndex(nextX, nextY);
                    var nextCost = currentCost + GetStepCost(map, currentX, currentY, direction);
                    if (nextCost >= distances[nextIndex])
                    {
                        continue;
                    }

                    distances[nextIndex] = nextCost;
                    queue.Enqueue(nextIndex, nextCost);
                }
            }

            return new BattleFlowField(
                map.Width,
                map.Height,
                BuildFlowCells(map, distances, moveLayer),
                targetX,
                targetY);
        }

        private static FlowCell[] BuildFlowCells(
            BattleTileMap map,
            uint[] distances,
            MoveLayer moveLayer)
        {
            var cells = new FlowCell[distances.Length];
            for (var index = 0; index < cells.Length; index++)
            {
                var distance = distances[index];
                if (distance == UnvisitedCost)
                {
                    cells[index] = new FlowCell
                    {
                        Cost = FlowCell.BlockedCost
                    };
                    continue;
                }

                var x = index % map.Width;
                var y = index / map.Width;
                var bestDirection = int2.zero;
                var bestCost = UnvisitedCost;

                if (distance == 0u)
                {
                    cells[index] = new FlowCell
                    {
                        Cost = 0
                    };
                    continue;
                }

                for (var directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                {
                    var direction = Directions[directionIndex];
                    var nextX = x + direction.x;
                    var nextY = y + direction.y;
                    if (!CanTraverse(map, x, y, nextX, nextY, moveLayer))
                    {
                        continue;
                    }

                    var nextIndex = map.GetIndex(nextX, nextY);
                    var nextDistance = distances[nextIndex];
                    if (nextDistance == UnvisitedCost)
                    {
                        continue;
                    }

                    var candidateCost = nextDistance + GetStepCost(map, nextX, nextY, direction);
                    if (candidateCost <= distance && candidateCost < bestCost)
                    {
                        bestCost = candidateCost;
                        bestDirection = direction;
                    }
                }

                cells[index] = new FlowCell
                {
                    Cost = distance >= FlowCell.BlockedCost
                        ? (ushort)(FlowCell.BlockedCost - 1)
                        : (ushort)distance,
                    DirX = (sbyte)bestDirection.x,
                    DirY = (sbyte)bestDirection.y
                };
            }

            return cells;
        }

        private static bool CanTraverse(
            BattleTileMap map,
            int fromX,
            int fromY,
            int toX,
            int toY,
            MoveLayer moveLayer)
        {
            if (!IsEnterable(map, toX, toY, moveLayer))
            {
                return false;
            }

            var deltaX = toX - fromX;
            var deltaY = toY - fromY;
            if (deltaX == 0 || deltaY == 0)
            {
                return true;
            }

            return IsEnterable(map, fromX + deltaX, fromY, moveLayer) &&
                   IsEnterable(map, fromX, fromY + deltaY, moveLayer);
        }

        private static bool IsEnterable(
            BattleTileMap map,
            int x,
            int y,
            MoveLayer moveLayer)
        {
            if (!map.IsInside(x, y))
            {
                return false;
            }

            var tile = map.GetTile(x, y);
            return tile.Walkable &&
                   (tile.AllowedMoveLayers & moveLayer) != 0;
        }

        private static uint GetStepCost(BattleTileMap map, int enteringX, int enteringY, int2 direction)
        {
            var tile = map.GetTile(enteringX, enteringY);
            var baseCost = math.max(1, tile.MoveCost);
            var diagonal = direction.x != 0 && direction.y != 0;
            return (uint)(baseCost * (diagonal ? 14 : 10));
        }

        private readonly struct QueueNode
        {
            public QueueNode(int index, uint cost)
            {
                Index = index;
                Cost = cost;
            }

            public int Index { get; }
            public uint Cost { get; }
        }

        private sealed class MinHeap
        {
            private readonly List<QueueNode> nodes = new List<QueueNode>();

            public void Enqueue(int index, uint cost)
            {
                nodes.Add(new QueueNode(index, cost));
                SiftUp(nodes.Count - 1);
            }

            public bool TryDequeue(out int index, out uint cost)
            {
                if (nodes.Count == 0)
                {
                    index = default;
                    cost = default;
                    return false;
                }

                var root = nodes[0];
                var lastIndex = nodes.Count - 1;
                nodes[0] = nodes[lastIndex];
                nodes.RemoveAt(lastIndex);
                if (nodes.Count > 0)
                {
                    SiftDown(0);
                }

                index = root.Index;
                cost = root.Cost;
                return true;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (nodes[parent].Cost <= nodes[index].Cost)
                    {
                        return;
                    }

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void SiftDown(int index)
            {
                while (true)
                {
                    var left = index * 2 + 1;
                    var right = left + 1;
                    var smallest = index;

                    if (left < nodes.Count && nodes[left].Cost < nodes[smallest].Cost)
                    {
                        smallest = left;
                    }

                    if (right < nodes.Count && nodes[right].Cost < nodes[smallest].Cost)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int left, int right)
            {
                var temporary = nodes[left];
                nodes[left] = nodes[right];
                nodes[right] = temporary;
            }
        }
    }
}
