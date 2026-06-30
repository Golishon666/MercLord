using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public interface IBattleViewSpawner
    {
        void SpawnMissingViews(BattleSession session, Transform parent);
        void ReleaseAll();
    }

    public sealed class BattleViewSpawner : IBattleViewSpawner
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IBattleViewFactory viewFactory;
        private readonly List<Entity> spawnBuffer = new List<Entity>();

        public BattleViewSpawner(
            ConfigDatabase configDatabase,
            IBattleViewFactory viewFactory)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public void SpawnMissingViews(BattleSession session, Transform parent)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var world = session.World;
            if (world == null || world.IsDisposed)
            {
                throw new InvalidOperationException("Cannot spawn battle views without an active Morpeh world.");
            }

            SpawnMissingUnitViews(world, parent);
            SpawnMissingVehicleViews(world, parent);
        }

        private void SpawnMissingUnitViews(World world, Transform parent)
        {
            spawnBuffer.Clear();
            var filter = world.Filter
                .With<BotComponent>()
                .With<PositionComponent>()
                .Without<ViewRefComponent>()
                .Build();

            foreach (var entity in filter)
            {
                spawnBuffer.Add(entity);
            }

            var botStash = world.GetStash<BotComponent>();
            var viewRefStash = world.GetStash<ViewRefComponent>();

            for (var entityIndex = 0; entityIndex < spawnBuffer.Count; entityIndex++)
            {
                var entity = spawnBuffer[entityIndex];
                var bot = botStash.Get(entity);
                if (!configDatabase.TryGetUnit(bot.UnitConfigId, out var unitConfig))
                {
                    throw new InvalidOperationException($"UnitConfig id {bot.UnitConfigId} is not registered.");
                }

                var viewId = viewFactory.SpawnUnitView(unitConfig, parent);
                viewRefStash.Set(entity, new ViewRefComponent { ViewId = viewId });
            }

            spawnBuffer.Clear();
            filter.Dispose();
        }

        private void SpawnMissingVehicleViews(World world, Transform parent)
        {
            spawnBuffer.Clear();
            var filter = world.Filter
                .With<VehicleComponent>()
                .With<PositionComponent>()
                .Without<ViewRefComponent>()
                .Build();

            foreach (var entity in filter)
            {
                spawnBuffer.Add(entity);
            }

            var vehicleStash = world.GetStash<VehicleComponent>();
            var viewRefStash = world.GetStash<ViewRefComponent>();

            for (var entityIndex = 0; entityIndex < spawnBuffer.Count; entityIndex++)
            {
                var entity = spawnBuffer[entityIndex];
                var vehicle = vehicleStash.Get(entity);
                if (!configDatabase.TryGetVehicle(vehicle.VehicleConfigId, out var vehicleConfig))
                {
                    throw new InvalidOperationException($"VehicleConfig id {vehicle.VehicleConfigId} is not registered.");
                }

                var viewId = viewFactory.SpawnVehicleView(vehicleConfig, parent);
                viewRefStash.Set(entity, new ViewRefComponent { ViewId = viewId });
            }

            spawnBuffer.Clear();
            filter.Dispose();
        }

        public void ReleaseAll()
        {
            viewFactory.ReleaseAll();
            spawnBuffer.Clear();
        }
    }
}
