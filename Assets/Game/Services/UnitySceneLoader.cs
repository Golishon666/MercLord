using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MercLord.Game.Services
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            return operation == null
                ? UniTask.CompletedTask
                : operation.ToUniTask(cancellationToken: cancellationToken);
        }

        public UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"Cannot unload scene because it is not loaded: {sceneName}");
                return UniTask.CompletedTask;
            }

            var operation = SceneManager.UnloadSceneAsync(scene);
            return operation == null
                ? UniTask.CompletedTask
                : operation.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
