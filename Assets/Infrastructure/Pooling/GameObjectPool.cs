using System;
using System.Collections.Generic;
using UnityEngine;

namespace MercLord.Infrastructure.Pooling
{
    public sealed class GameObjectPool
    {
        private readonly IPrefabFactory prefabFactory;
        private readonly GameObject prefab;
        private readonly Transform inactiveParent;
        private readonly Stack<GameObject> available = new Stack<GameObject>();

        public GameObjectPool(
            IPrefabFactory prefabFactory,
            GameObject prefab,
            Transform inactiveParent = null)
        {
            this.prefabFactory = prefabFactory ?? throw new ArgumentNullException(nameof(prefabFactory));
            this.prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            this.inactiveParent = inactiveParent;
        }

        public GameObject Rent(Transform activeParent = null)
        {
            var instance = GetAvailableInstance();
            instance.transform.SetParent(activeParent, worldPositionStays: false);
            instance.SetActive(true);
            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(inactiveParent, worldPositionStays: false);
            available.Push(instance);
        }

        private GameObject GetAvailableInstance()
        {
            while (available.Count > 0)
            {
                var pooled = available.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            return prefabFactory.Instantiate(prefab, inactiveParent);
        }
    }
}
