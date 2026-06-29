using System;
using UnityEngine;

namespace MercLord.Infrastructure.Pooling
{
    public sealed class PrefabFactory : IPrefabFactory
    {
        public T Instantiate<T>(T prefab, Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            return UnityEngine.Object.Instantiate(prefab, parent);
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            return UnityEngine.Object.Instantiate(prefab, parent);
        }
    }
}
