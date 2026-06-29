using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class SpatialHashSystem : IBattleRuntimeSystem
    {
        private readonly ConfigDatabase configDatabase;
        private readonly Dictionary<long, List<Entity>> buckets = new Dictionary<long, List<Entity>>();

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<TeamComponent> teams;
        private float cellSize;

        public SpatialHashSystem(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

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
                .Build();

            positions = world.GetStash<PositionComponent>();
            teams = world.GetStash<TeamComponent>();
            Rebuild();
        }

        public void Tick(float deltaTime)
        {
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
            cellSize = 0f;
        }

        public void GetOpponentsInRange(
            float2 center,
            float radius,
            BattleTeamType ownTeam,
            List<Entity> results)
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
                        if (!world.Has(entity) || !positions.Has(entity) || !teams.Has(entity))
                        {
                            continue;
                        }

                        var team = teams.Get(entity);
                        if (team.Value == ownTeam)
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

                bucket.Add(entity);
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
    }
}
