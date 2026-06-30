using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace MercLord.Game.Services
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        private readonly LifetimeScope parentScope;

        public UnitySceneLoader(LifetimeScope parentScope)
        {
            this.parentScope = parentScope;
        }

        public async UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default)
        {
            if (parentScope == null)
            {
                await LoadSceneOperationAsync(sceneName, mode, cancellationToken);
                return;
            }

            using (LifetimeScope.EnqueueParent(parentScope))
            {
                await LoadSceneOperationAsync(sceneName, mode, cancellationToken);
            }
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

        private static UniTask LoadSceneOperationAsync(
            string sceneName,
            LoadSceneMode mode,
            CancellationToken cancellationToken)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            return operation == null
                ? UniTask.CompletedTask
                : operation.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
