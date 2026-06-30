using System;
using System.Collections.Generic;
using MercLord.Game.Configs;
using MercLord.Infrastructure.Pooling;
using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public interface IBattleViewFactory
    {
        int SpawnUnitView(UnitConfig unitConfig, Transform parent);
        int SpawnVehicleView(VehicleConfig vehicleConfig, Transform parent);
        bool TryGetView(int viewId, out GameObject view);
        void ReleaseView(int viewId);
        void ReleaseAll();
    }

    public sealed class BattleViewFactory : IBattleViewFactory
    {
        private readonly IPrefabFactory prefabFactory;
        private readonly BattleViewCatalog viewCatalog;
        private readonly BattleViewRegistry viewRegistry;
        private readonly Dictionary<string, GameObjectPool> unitViewPools =
            new Dictionary<string, GameObjectPool>(StringComparer.Ordinal);

        public BattleViewFactory(
            IPrefabFactory prefabFactory,
            BattleViewCatalog viewCatalog,
            BattleViewRegistry viewRegistry)
        {
            this.prefabFactory = prefabFactory ?? throw new ArgumentNullException(nameof(prefabFactory));
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
            this.viewRegistry = viewRegistry ?? throw new ArgumentNullException(nameof(viewRegistry));
        }

        public int SpawnUnitView(UnitConfig unitConfig, Transform parent)
        {
            if (unitConfig == null)
            {
                throw new ArgumentNullException(nameof(unitConfig));
            }

            if (!viewCatalog.TryGetUnitViewPrefab(unitConfig.ViewPrefabAddress, out var prefab))
            {
                throw new InvalidOperationException(
                    $"{unitConfig.DisplayName} references missing unit view prefab address '{unitConfig.ViewPrefabAddress}'.");
            }

            var pool = GetUnitViewPool(unitConfig.ViewPrefabAddress, prefab, parent);
            var view = pool.Rent(parent);
            return viewRegistry.Register(view, pool);
        }

        public int SpawnVehicleView(VehicleConfig vehicleConfig, Transform parent)
        {
            if (vehicleConfig == null)
            {
                throw new ArgumentNullException(nameof(vehicleConfig));
            }

            if (!viewCatalog.TryGetVehicleViewPrefab(vehicleConfig.ViewPrefabAddress, out var prefab))
            {
                throw new InvalidOperationException(
                    $"{vehicleConfig.DisplayName} references missing vehicle view prefab address '{vehicleConfig.ViewPrefabAddress}'.");
            }

            var pool = GetUnitViewPool(vehicleConfig.ViewPrefabAddress, prefab, parent);
            var view = pool.Rent(parent);
            return viewRegistry.Register(view, pool);
        }

        public bool TryGetView(int viewId, out GameObject view)
        {
            return viewRegistry.TryGetView(viewId, out view);
        }

        public void ReleaseView(int viewId)
        {
            viewRegistry.Release(viewId);
        }

        public void ReleaseAll()
        {
            viewRegistry.ReleaseAll();
        }

        private GameObjectPool GetUnitViewPool(
            string address,
            GameObject prefab,
            Transform inactiveParent)
        {
            if (!unitViewPools.TryGetValue(address, out var pool))
            {
                pool = new GameObjectPool(prefabFactory, prefab, inactiveParent);
                unitViewPools.Add(address, pool);
            }

            return pool;
        }
    }
}
