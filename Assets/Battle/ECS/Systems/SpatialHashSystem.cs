using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public readonly struct SpatialHashBucketDebugInfo
    {
        public SpatialHashBucketDebugInfo(int cellX, int cellY, int entityCount, float cellSize)
        {
            CellX = cellX;
            CellY = cellY;
            EntityCount = Math.Max(0, entityCount);
            CellSize = Math.Max(0f, cellSize);
        }

        public int CellX { get; }
        public int CellY { get; }
        public int EntityCount { get; }
        public float CellSize { get; }
        public float2 Min => new float2(CellX * CellSize, CellY * CellSize);
        public float2 Max => new float2((CellX + 1) * CellSize, (CellY + 1) * CellSize);
    }

    public sealed class SpatialHashSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.05f;

        private readonly ConfigDatabase configDatabase;
        private readonly Dictionary<long, List<Entity>> buckets = new Dictionary<long, List<Entity>>();

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<TeamComponent> teams;
        private Stash<DeadComponent> dead;
        private Stash<DriverComponent> drivers;
        private float cellSize;
        private BattleCadenceTimer tickTimer;

        public SpatialHashSystem(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public int ActiveBucketCount { get; private set; }
        public int IndexedEntityCount { get; private set; }
        public float CellSize => cellSize;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var simulationConfig = configDatabase.BattleSimulation
                ?? throw new InvalidOperationException("SpatialHashSystem requires BattleSimulationConfig.");
            if (simulationConfig.SpatialHashCellSize <= 0f)
            {
                throw new InvalidOperationException("Battle simulation spatial hash cell size must be positive.");
            }

            cellSize = simulationConfig.SpatialHashCellSize;
            world = session.World ?? throw new InvalidOperationException("SpatialHashSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("SpatialHashSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Without<DriverComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            teams = world.GetStash<TeamComponent>();
            dead = world.GetStash<DeadComponent>();
            drivers = world.GetStash<DriverComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
            Rebuild();
            tickTimer.Consume(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            Rebuild();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            foreach (var bucket in buckets.Values)
            {
                bucket.Clear();
            }

            buckets.Clear();
            filter = null;
            world = null;
            positions = null;
            teams = null;
            dead = null;
            drivers = null;
            cellSize = 0f;
            tickTimer = default;
            ActiveBucketCount = 0;
            IndexedEntityCount = 0;
        }

        public void GetOpponentsInRange(
            float2 center,
            float radius,
            BattleTeamType ownTeam,
            List<Entity> results)
        {
            QueryInRange(
                center,
                radius,
                results,
                entity => teams.Get(entity).Value != ownTeam);
        }

        public void GetEntitiesInRange(
            float2 center,
            float radius,
            List<Entity> results)
        {
            QueryInRange(center, radius, results, null);
        }

        public void GetDebugBuckets(List<SpatialHashBucketDebugInfo> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (world == null || world.IsDisposed || cellSize <= 0f)
            {
                return;
            }

            foreach (var pair in buckets)
            {
                var bucket = pair.Value;
                if (bucket == null || bucket.Count == 0)
                {
                    continue;
                }

                DecodeKey(pair.Key, out var cellX, out var cellY);
                results.Add(new SpatialHashBucketDebugInfo(cellX, cellY, bucket.Count, cellSize));
            }
        }

        private void QueryInRange(
            float2 center,
            float radius,
            List<Entity> results,
            Func<Entity, bool> predicate)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (world == null || world.IsDisposed || radius <= 0f)
            {
                return;
            }

            var minCell = ToCell(center - new float2(radius, radius));
            var maxCell = ToCell(center + new float2(radius, radius));
            var radiusSquared = radius * radius;

            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    if (!buckets.TryGetValue(ToKey(x, y), out var bucket))
                    {
                        continue;
                    }

                    for (var entityIndex = 0; entityIndex < bucket.Count; entityIndex++)
                    {
                        var entity = bucket[entityIndex];
                        if (!world.Has(entity) ||
                            !positions.Has(entity) ||
                            !teams.Has(entity) ||
                            dead.Has(entity) ||
                            drivers.Has(entity))
                        {
                            continue;
                        }

                        if (predicate != null && !predicate(entity))
                        {
                            continue;
                        }

                        var position = positions.Get(entity);
                        if (math.distancesq(center, position.Value) <= radiusSquared)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }

        private void Rebuild()
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            foreach (var bucket in buckets.Values)
            {
                bucket.Clear();
            }

            ActiveBucketCount = 0;
            IndexedEntityCount = 0;
            foreach (var entity in filter)
            {
                var position = positions.Get(entity);
                var cell = ToCell(position.Value);
                var key = ToKey(cell.x, cell.y);
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new List<Entity>();
                    buckets.Add(key, bucket);
                }

                if (bucket.Count == 0)
                {
                    ActiveBucketCount++;
                }

                bucket.Add(entity);
                IndexedEntityCount++;
            }
        }

        private int2 ToCell(float2 position)
        {
            return new int2(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize));
        }

        private static long ToKey(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }

        private static void DecodeKey(long key, out int x, out int y)
        {
            x = (int)(key >> 32);
            y = unchecked((int)(key & 0xffffffff));
        }
    }
}
