using System;
using System.Collections.Generic;
using MercLord.Infrastructure.Pooling;
using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleViewRegistry
    {
        private readonly Dictionary<int, PooledBattleView> activeViews = new Dictionary<int, PooledBattleView>();
        private int nextViewId = 1;

        public int Register(GameObject view, GameObjectPool pool)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            var viewId = nextViewId++;
            activeViews.Add(viewId, new PooledBattleView(view, pool));
            return viewId;
        }

        public bool TryGetView(int viewId, out GameObject view)
        {
            if (activeViews.TryGetValue(viewId, out var handle))
            {
                view = handle.View;
                return view != null;
            }

            view = null;
            return false;
        }

        public void Release(int viewId)
        {
            if (!activeViews.TryGetValue(viewId, out var handle))
            {
                return;
            }

            activeViews.Remove(viewId);
            handle.Pool.Return(handle.View);
        }

        public void ReleaseAll()
        {
            foreach (var handle in activeViews.Values)
            {
                handle.Pool.Return(handle.View);
            }

            activeViews.Clear();
        }

        private readonly struct PooledBattleView
        {
            public PooledBattleView(GameObject view, GameObjectPool pool)
            {
                View = view;
                Pool = pool;
            }

            public GameObject View { get; }
            public GameObjectPool Pool { get; }
        }
    }
}
