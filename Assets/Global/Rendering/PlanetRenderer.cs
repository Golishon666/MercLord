using MercLord.Global.Cells;
using System.Collections.Generic;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class PlanetRenderer : MonoBehaviour
    {
        [SerializeField] private GlobalMapViewSettings settings;
        private readonly List<Component> spawnedViews = new List<Component>();

        public GlobalMapViewSettings Settings => settings;

        public void Bind(WorldModel worldModel)
        {
            CurrentWorld = worldModel;
        }

        public WorldModel CurrentWorld { get; private set; }

        public void TrackSpawnedView(Component view)
        {
            if (view != null)
            {
                spawnedViews.Add(view);
            }
        }

        public void ClearSpawnedViews()
        {
            for (var viewIndex = 0; viewIndex < spawnedViews.Count; viewIndex++)
            {
                var view = spawnedViews[viewIndex];
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            spawnedViews.Clear();
        }
    }
}
