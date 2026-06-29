using UnityEngine;

namespace MercLord.Infrastructure.Pooling
{
    public interface IPrefabFactory
    {
        T Instantiate<T>(T prefab, Transform parent = null)
            where T : Component;

        GameObject Instantiate(GameObject prefab, Transform parent = null);
    }
}
