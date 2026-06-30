using System;
using Cysharp.Threading.Tasks;
using MercLord.Game.StateMachine;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MercLord.Game.UI
{
    public sealed class MainMenuSceneRoot : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;

        private IGameStateMachine stateMachine;
        private bool isChangingState;

        public Button NewGameButton => newGameButton;

        [Inject]
        public void Construct(IGameStateMachine injectedStateMachine)
        {
            stateMachine = injectedStateMachine ?? throw new ArgumentNullException(nameof(injectedStateMachine));
        }

        private void OnEnable()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(StartNewGame);
            }
        }

        private void OnDisable()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(StartNewGame);
            }
        }

        private void StartNewGame()
        {
            if (stateMachine == null || isChangingState)
            {
                return;
            }

            StartNewGameAsync().Forget();
        }

        private async UniTask StartNewGameAsync()
        {
            try
            {
                isChangingState = true;
                await stateMachine.ChangeStateAsync(GameStateId.GenerateWorld, new NewGameRequest());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                isChangingState = false;
            }
        }
    }
}
