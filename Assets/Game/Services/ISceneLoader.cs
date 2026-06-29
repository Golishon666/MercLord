using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MercLord.Game.Services
{
    public interface ISceneLoader
    {
        UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default);

        UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
